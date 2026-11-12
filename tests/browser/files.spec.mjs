import { test, expect } from "@playwright/test";
import { startFirstPeriod, recordSession } from "./helpers.mjs";

const key = "flexisheet.workspace.v1";
const retainedId = "00000000-0000-0000-0000-000000000002";
const expectedTime = Array.from({ length: 28 }, (_, index) => index % 7 < 5 ? "07:30:00" : "00:00:00");

function importFile() {
    return {
        schemaVersion: 2,
        workingPattern: expectedTime,
        periods: [
            {
                id: "00000000-0000-0000-0000-000000000001",
                startDate: "2020-01-06", expectedTime, carryOver: "00:00:00", sessions: []
            },
            {
                id: retainedId, startDate: "2026-10-05", expectedTime, carryOver: "-02:00:00",
                sessions: [{
                    id: "00000000-0000-0000-0000-000000000003",
                    date: "2026-10-05", startTime: "23:00:00", endTime: "00:00:00", endsAtMidnight: true
                }]
            }
        ]
    };
}

async function chooseFile(page, data, name = "backup.json") {
    await page.locator("#backup-file").setInputFiles({
        name, mimeType: "application/json",
        buffer: Buffer.from(typeof data === "string" ? data : JSON.stringify(data))
    });
}

async function downloadedJson(download) {
    const stream = await download.createReadStream();
    const chunks = [];
    for await (const chunk of stream) chunks.push(chunk);
    return JSON.parse(Buffer.concat(chunks).toString("utf8"));
}

test.beforeEach(async ({ page }) => {
    await page.clock.setFixedTime(new Date("2026-11-12T12:00:00Z"));
});

test("backup downloads all retained data with its schema and working pattern", async ({ page }) => {
    await startFirstPeriod(page, "-2:00");
    await recordSession(page, "09:00", "17:00");
    await page.getByRole("link", { name: "Backup and restore", exact: true }).click();
    const promised = page.waitForEvent("download");
    await page.getByRole("button", { name: "Download backup", exact: true }).click();
    const download = await promised;
    expect(download.suggestedFilename()).toBe("flexisheet-backup-2026-11-12.json");
    const backup = await downloadedJson(download);
    expect(backup.schemaVersion).toBe(2);
    expect(backup.workingPattern).toEqual(expectedTime);
    expect(backup.periods).toHaveLength(1);
    expect(backup.periods[0].carryOver).toBe("-02:00:00");
    expect(backup.periods[0].sessions).toHaveLength(1);
    const saved = await page.evaluate(key => JSON.parse(localStorage.getItem(key)), key);
    expect(backup).toEqual(saved);
});

test("invalid files and cancellation preserve data; confirmed restore replaces and purges", async ({ page }) => {
    await startFirstPeriod(page);
    const original = await page.evaluate(key => localStorage.getItem(key), key);
    await page.getByRole("link", { name: "Backup and restore", exact: true }).click();
    const invalidOverlap = importFile();
    invalidOverlap.periods[1].sessions.push({
        ...invalidOverlap.periods[1].sessions[0],
        id: "00000000-0000-0000-0000-000000000004"
    });
    for (const invalid of ["not-json", { schemaVersion: 99, workingPattern: null, periods: [] }, invalidOverlap]) {
        await chooseFile(page, invalid);
        await expect(page.getByRole("alert")).toBeVisible();
        await expect(page.getByRole("button", { name: "Replace existing data" })).toHaveCount(0);
        expect(await page.evaluate(key => localStorage.getItem(key), key)).toBe(original);
    }

    await chooseFile(page, importFile());
    await expect(page.getByTestId("restore-preview")).toContainText("Retained periods to restore: 1. Expired periods excluded: 1.");
    expect(await page.evaluate(key => localStorage.getItem(key), key)).toBe(original);
    await page.getByRole("button", { name: "Cancel restore" }).click();
    await expect(page.getByRole("button", { name: "Replace existing data" })).toHaveCount(0);
    expect(await page.evaluate(key => localStorage.getItem(key), key)).toBe(original);
    await chooseFile(page, importFile());
    await expect(page.getByRole("button", { name: "Replace existing data" })).toBeVisible();
    await page.screenshot({ path: "artifacts/screenshots/backup-restore.png", fullPage: true });
    await page.getByRole("button", { name: "Replace existing data" }).click();
    await expect(page.getByRole("status").filter({ hasText: "Your backup has been restored" })).toBeVisible();
    const restored = await page.evaluate(key => JSON.parse(localStorage.getItem(key)), key);
    expect(restored.periods.map(period => period.id)).toEqual([retainedId]);
    expect(restored.workingPattern).toEqual(expectedTime);
    expect(restored.periods[0].carryOver).toBe("-02:00:00");
    await page.getByRole("link", { name: "Return to flexi periods" }).click();
    await expect(page.getByText("Worked: 1:00 | Balance: -151:00", { exact: true })).toBeVisible();
    await page.reload();
    await expect(page.getByRole("button", { name: "5 Oct 2026 to 1 Nov 2026" })).toBeVisible();
    await expect(page.getByRole("button", { name: "2 Nov 2026 to 29 Nov 2026" })).toHaveCount(0);
});

test("completed-period email downloads only that period before opening a manual-attachment draft", async ({ page }) => {
    await startFirstPeriod(page);
    await page.getByLabel("Starting Monday").fill("2026-10-05");
    await page.getByLabel("Carry-over", { exact: true }).fill("-2:00");
    await page.getByRole("button", { name: "Start period", exact: true }).click();
    await recordSession(page, "23:00", "24:00");
    await expect(page.getByRole("button", { name: "Download period starting 2 Nov 2026" })).toHaveCount(0);
    await expect(page.getByRole("button", { name: "Email period starting 2 Nov 2026" })).toHaveCount(0);

    await page.evaluate(() => {
        window.deliveryEvents = [];
        const download = window.flexiSheetFiles.download;
        window.flexiSheetFiles.download = (...args) => {
            window.deliveryEvents.push("download");
            if (window.blockDownloads) throw new Error("Download blocked");
            return download(...args);
        };
        window.flexiSheetFiles.email = mailto => {
            window.deliveryEvents.push("email");
            window.composedEmail = mailto;
        };
    });
    const firstDownload = page.waitForEvent("download");
    await page.getByRole("button", { name: "Download period starting 5 Oct 2026" }).click();
    const file = await firstDownload;
    expect(file.suggestedFilename()).toBe("flexisheet-period-2026-10-05.json");
    const standalone = await downloadedJson(file);
    expect(standalone.schemaVersion).toBe(2);
    expect(standalone.workingPattern).toBeNull();
    expect(standalone.periods).toHaveLength(1);
    expect(standalone.periods[0].startDate).toBe("2026-10-05");
    expect(standalone.periods[0].sessions[0].endsAtMidnight).toBe(true);

    await page.evaluate(() => { window.deliveryEvents = []; });
    const emailDownload = page.waitForEvent("download");
    await page.getByRole("button", { name: "Email period starting 5 Oct 2026" }).click();
    expect(await downloadedJson(await emailDownload)).toEqual(standalone);
    await expect.poll(() => page.evaluate(() => window.deliveryEvents)).toEqual(["download", "email"]);
    const uri = new URL(await page.evaluate(() => window.composedEmail));
    expect(uri.protocol).toBe("mailto:");
    expect([...uri.searchParams.keys()]).toEqual(["subject", "body"]);
    expect(uri.searchParams.get("subject")).toBe("Flexi sheet for period starting 2026-10-05");
    expect(uri.searchParams.get("body")).toContain("Worked: 1:00");
    expect(uri.searchParams.get("body")).toContain("Carry-over: -2:00");
    expect(uri.searchParams.get("body")).toContain("Balance: -151:00");
    expect(uri.searchParams.get("body")).toContain("Please attach the downloaded file flexisheet-period-2026-10-05.json before sending.");

    await page.evaluate(() => {
        window.composedEmail = null;
        window.blockDownloads = true;
    });
    await page.getByRole("button", { name: "Email period starting 5 Oct 2026" }).click();
    await expect(page.getByRole("alert")).toContainText("file download could not be started");
    expect(await page.evaluate(() => window.composedEmail)).toBeNull();
});

test("restore rejects stale confirmation rather than overwriting another tab", async ({ page, context }) => {
    await startFirstPeriod(page);
    await page.getByRole("link", { name: "Backup and restore", exact: true }).click();
    await chooseFile(page, importFile());
    await expect(page.getByRole("button", { name: "Replace existing data" })).toBeVisible();
    const other = await context.newPage();
    await other.goto("settings");
    await expect(other.locator("#pattern-0")).toHaveValue("7:30");
    await other.locator("#pattern-0").fill("4:00");
    await other.getByRole("button", { name: "Save working pattern" }).click();
    await expect(other.getByRole("status").filter({ hasText: "Your normal working pattern has been saved" })).toBeVisible();
    const changed = await other.evaluate(key => localStorage.getItem(key), key);
    await page.getByRole("button", { name: "Replace existing data" }).click();
    await expect(page.getByRole("alert")).toContainText("Another tab changed your data");
    expect(await page.evaluate(key => localStorage.getItem(key), key)).toBe(changed);
});

test("a confirmed restore can recover unreadable browser data", async ({ page }) => {
    await page.addInitScript(({ key }) => {
        if (localStorage.getItem(key) === null) localStorage.setItem(key, "corrupt");
    }, { key });
    await page.goto("./");
    await expect(page.getByRole("heading", { name: "Your data could not be opened" })).toBeVisible();
    await page.getByRole("link", { name: "Restore a backup", exact: true }).click();
    await expect(page.getByRole("button", { name: "Download backup", exact: true })).toBeDisabled();
    await chooseFile(page, importFile());
    await expect(page.getByRole("button", { name: "Replace existing data" })).toBeVisible();
    expect(await page.evaluate(key => localStorage.getItem(key), key)).toBe("corrupt");
    await page.getByRole("button", { name: "Replace existing data" }).click();
    await expect(page.getByRole("status").filter({ hasText: "Your backup has been restored" })).toBeVisible();
    await page.getByRole("link", { name: "Return to flexi periods" }).click();
    await expect(page.getByRole("button", { name: "5 Oct 2026 to 1 Nov 2026" })).toBeVisible();
});

test("failed restore writes preserve the existing data and do not claim success", async ({ page }) => {
    await startFirstPeriod(page);
    const original = await page.evaluate(key => localStorage.getItem(key), key);
    await page.getByRole("link", { name: "Backup and restore", exact: true }).click();
    await chooseFile(page, importFile());
    await expect(page.getByRole("button", { name: "Replace existing data" })).toBeVisible();
    await page.evaluate(() => {
        Storage.prototype.setItem = () => { throw new DOMException("Full", "QuotaExceededError"); };
    });
    await page.getByRole("button", { name: "Replace existing data" }).click();
    await expect(page.getByRole("alert")).toContainText("could not be saved");
    await expect(page.getByRole("status").filter({ hasText: "Your backup has been restored" })).toHaveCount(0);
    expect(await page.evaluate(key => localStorage.getItem(key), key)).toBe(original);
});
