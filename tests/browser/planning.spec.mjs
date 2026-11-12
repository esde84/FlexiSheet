import { test, expect } from "@playwright/test";
import { savePattern } from "./helpers.mjs";

const key = "flexisheet.workspace.v1";

test.beforeEach(async ({ page }) => {
    await page.clock.setFixedTime(new Date("2026-11-12T12:00:00Z"));
});

test("configure, start, adjust and reload a period under the GitHub Pages subpath", async ({ page }) => {
    const errors = [];
    page.on("pageerror", error => errors.push(error.message));
    await page.goto("./");
    await expect(page.getByText("Set your normal four-week working pattern")).toBeVisible();
    await savePattern(page);
    await page.getByRole("link", { name: "Return to flexi periods" }).click();
    await page.getByLabel("Starting Monday").fill("2026-11-02");
    await page.getByLabel("Carry-over", { exact: true }).fill("-2:15");
    await page.getByRole("button", { name: "Start period", exact: true }).click();
    await expect(page.getByText("Expected: 150:00 | Carry-over: -2:15", { exact: true })).toBeVisible();
    await page.locator("#period-0").fill("3:00");
    await page.getByLabel("Carry-over for this period").fill("120:30");
    await page.getByRole("button", { name: "Save period changes" }).click();
    await expect(page.getByText("Expected: 145:30 | Carry-over: 120:30", { exact: true })).toBeVisible();
    await page.reload();
    await expect(page.getByText("Expected: 145:30 | Carry-over: 120:30", { exact: true })).toBeVisible();
    await page.getByRole("button", { name: "2 Nov 2026 to 29 Nov 2026" }).click();
    await expect(page.locator("#period-0")).toHaveValue("3:00");
    await page.getByRole("link", { name: "Working pattern", exact: true }).click();
    await expect(page.locator("#pattern-0")).toHaveValue("7:30");
    const skip = page.getByRole("link", { name: "Skip to main content" });
    await skip.focus();
    await skip.press("Enter");
    await expect(page).toHaveURL(/\/FlexiSheet\/settings$/);
    await expect(page.locator("main")).toBeFocused();
    const css = await page.evaluate(async () => await (await fetch("css/app.css")).text());
    expect(css).not.toMatch(/GDS Transport|@font-face|url\(/i);
    expect(errors).toEqual([]);
});

test("invalid inputs do not change saved periods", async ({ page }) => {
    await savePattern(page);
    await page.goto("./");
    await page.getByLabel("Starting Monday").fill("2026-11-03");
    await page.getByRole("button", { name: "Start period", exact: true }).click();
    await expect(page.getByRole("alert")).toContainText("must start on a Monday");
    await expect(page.getByRole("alert")).toBeFocused();
    await expect(page.getByText("You have no saved periods.")).toBeVisible();
    await page.getByLabel("Starting Monday").fill("2026-11-02");
    await page.getByLabel("Carry-over", { exact: true }).fill("1:90");
    await page.getByRole("button", { name: "Start period", exact: true }).click();
    await expect(page.getByRole("alert")).toContainText("Enter carry-over");
    await page.getByLabel("Carry-over", { exact: true }).fill("0:00");
    await page.getByRole("button", { name: "Start period", exact: true }).click();
    await expect(page.getByRole("heading", { name: "Adjust this period" })).toBeVisible();
    await page.locator("#period-0").fill("24:01");
    await page.getByRole("button", { name: "Save period changes" }).click();
    await expect(page.getByRole("alert")).toContainText("between 0:00 and 24:00");
    await page.reload();
    await expect(page.getByText("Expected: 150:00 | Carry-over: 0:00", { exact: true })).toBeVisible();
});

test("corrupt data is preserved and blocks editing", async ({ page }) => {
    await page.addInitScript(({ key }) => localStorage.setItem(key, "not-json"), { key });
    await page.goto("./");
    await expect(page.getByRole("heading", { name: "Your data could not be opened" })).toBeVisible();
    await expect(page.getByRole("alert")).toContainText("Existing browser data has not been changed");
    expect(await page.evaluate(key => localStorage.getItem(key), key)).toBe("not-json");
    await expect(page.getByRole("button", { name: "Start period", exact: true })).toHaveCount(0);
});

test("storage failure is visible and never claims a save", async ({ page }) => {
    await page.addInitScript(() => {
        Storage.prototype.setItem = () => { throw new DOMException("Full", "QuotaExceededError"); };
    });
    await page.goto("settings");
    await expect(page.getByRole("heading", { name: "Normal four-week working pattern" })).toBeVisible();
    await page.getByRole("button", { name: "Save working pattern" }).click();
    await expect(page.getByRole("alert")).toContainText("could not be saved");
    await expect(page.getByText("Your normal working pattern has been saved.", { exact: true })).toHaveCount(0);
});

test("another tab cannot silently replace a stale form", async ({ page, context }) => {
    await savePattern(page);
    const other = await context.newPage();
    await other.goto("settings");
    await expect(other.locator("#pattern-0")).toHaveValue("7:30");
    await page.locator("#pattern-0").fill("4:00");
    await page.getByRole("button", { name: "Save working pattern" }).click();
    await expect(page.getByRole("status").filter({ hasText: "Your normal working pattern has been saved" })).toBeVisible();
    await other.locator("#pattern-0").fill("6:00");
    await other.getByRole("button", { name: "Save working pattern" }).click();
    await expect(other.getByRole("alert")).toContainText("Another tab changed your data");
    await other.reload();
    await expect(other.locator("#pattern-0")).toHaveValue("4:00");
});

test("startup purges expired periods but retains the exact two-year boundary", async ({ page }) => {
    await page.clock.setFixedTime(new Date("2026-11-10T12:00:00Z"));
    await page.addInitScript(({ key }) => {
        const expectedTime = Array(28).fill("00:00:00");
        localStorage.setItem(key, JSON.stringify({
            schemaVersion: 1,
            workingPattern: expectedTime,
            periods: [
                { id: "00000000-0000-0000-0000-000000000001", startDate: "2024-09-16", expectedTime, carryOver: "00:00:00" },
                { id: "00000000-0000-0000-0000-000000000002", startDate: "2024-10-14", expectedTime, carryOver: "00:00:00" }
            ]
        }));
    }, { key });
    await page.goto("./");
    await expect(page.getByRole("button", { name: "14 Oct 2024 to 10 Nov 2024" })).toBeVisible();
    await expect(page.getByRole("button", { name: "16 Sept 2024 to 13 Oct 2024" })).toHaveCount(0);
    const saved = await page.evaluate(key => JSON.parse(localStorage.getItem(key)), key);
    expect(saved.periods.map(period => period.startDate)).toEqual(["2024-10-14"]);
});
