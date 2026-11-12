import { test, expect } from "@playwright/test";
import { startFirstPeriod, recordSession } from "./helpers.mjs";

test.beforeEach(async ({ page }) => {
    await page.clock.setFixedTime(new Date("2026-11-12T12:00:00Z"));
});

test("record adjacent sessions, reject overlap, edit and delete with derived totals", async ({ page }) => {
    const errors = [];
    page.on("pageerror", error => errors.push(error.message));
    await startFirstPeriod(page);
    await recordSession(page, "09:00", "12:00");
    await expect(page.getByTestId("day-balance")).toContainText("Worked: 3:00 | Expected: 7:30 | Difference: -4:30");
    await recordSession(page, "12:00", "13:00");
    await expect(page.getByTestId("period-balance")).toContainText("balance -146:00");
    await expect(page.getByRole("table").getByRole("row").filter({ hasText: "2 Nov 2026" })).toContainText("4:00");
    await expect(page.getByRole("table").getByRole("row").filter({ hasText: "2 Nov 2026" })).toContainText("-33:30");
    await page.screenshot({ path: "artifacts/screenshots/period-planning.png", fullPage: true });

    await recordSession(page, "11:00", "14:00");
    await expect(page.getByRole("alert")).toContainText("must not overlap");
    await expect(page.locator(".session-list > li")).toHaveCount(2);
    const morning = page.locator(".session-list > li").filter({ hasText: "09:00 to 12:00" });
    await morning.getByRole("button", { name: "Edit", exact: true }).click();
    await page.getByLabel("End time", { exact: true }).fill("12:30");
    await page.getByRole("button", { name: "Save session changes" }).click();
    await expect(page.getByRole("alert")).toContainText("must not overlap");
    await page.getByLabel("End time", { exact: true }).fill("11:00");
    await page.getByRole("button", { name: "Save session changes" }).click();
    await expect(page.getByTestId("day-balance")).toContainText("Worked: 3:00");
    const afternoon = page.locator(".session-list > li").filter({ hasText: "12:00 to 13:00" });
    await afternoon.getByRole("button", { name: "Delete", exact: true }).click();
    await afternoon.getByRole("button", { name: "Keep session" }).click();
    await expect(page.locator(".session-list > li")).toHaveCount(2);
    await afternoon.getByRole("button", { name: "Delete", exact: true }).click();
    await afternoon.getByRole("button", { name: "Confirm deletion" }).click();
    await expect(page.getByTestId("day-balance")).toContainText("Worked: 2:00");
    await page.reload();
    await page.getByRole("button", { name: "2 Nov 2026 to 29 Nov 2026" }).click();
    await expect(page.getByTestId("day-balance")).toContainText("Worked: 2:00");
    await expect(page.locator(".session-list > li")).toHaveCount(1);
    await expect(page.locator(".session-list")).toContainText("09:00 to 11:00");
    expect(errors).toEqual([]);
});

test("midnight work is split between dates with an explicit 24:00 ending", async ({ page }) => {
    await startFirstPeriod(page);
    await recordSession(page, "23:00", "01:00");
    await expect(page.getByRole("alert")).toContainText("end time must be after the start");
    await recordSession(page, "23:00", "00:00");
    await expect(page.getByRole("alert")).toContainText("Use 24:00");
    await recordSession(page, "23:00", "24:00");
    await expect(page.locator(".session-list")).toContainText("23:00 to 24:00 (1:00)");
    await page.getByLabel("Work date").fill("2026-11-03");
    await recordSession(page, "00:00", "01:00");
    await expect(page.getByTestId("day-balance")).toContainText("Worked: 1:00");
    await expect(page.getByTestId("period-balance")).toContainText("Worked 2:00");
});

test("calculated carry-over stays fixed after earlier edits and can be overridden manually", async ({ page }) => {
    await startFirstPeriod(page, "2:00");
    await recordSession(page, "09:00", "17:00");
    await expect(page.getByTestId("period-balance")).toContainText("balance -140:00");
    await page.getByLabel("Starting Monday").fill("2026-11-30");
    await expect(page.getByLabel("Use the previous period's balance")).toBeChecked();
    await page.getByRole("button", { name: "Start period", exact: true }).click();
    await expect(page.getByText("Expected: 150:00 | Carry-over: -140:00", { exact: true })).toBeVisible();

    await page.getByRole("button", { name: "2 Nov 2026 to 29 Nov 2026" }).click();
    await page.getByLabel("Work date").fill("2026-11-03");
    await recordSession(page, "09:00", "17:00");
    await expect(page.getByTestId("period-balance")).toContainText("balance -132:00");
    await expect(page.getByText("Expected: 150:00 | Carry-over: -140:00", { exact: true })).toBeVisible();
    await page.getByRole("button", { name: "30 Nov 2026 to 27 Dec 2026" }).click();
    await page.getByLabel("Carry-over for this period").fill("-3:00");
    await page.getByRole("button", { name: "Save period changes" }).click();
    await expect(page.getByText("Expected: 150:00 | Carry-over: -3:00", { exact: true })).toBeVisible();
    await page.getByLabel("Starting Monday").fill("2026-12-28");
    await page.getByLabel("Use the previous period's balance").uncheck();
    await page.getByLabel("Carry-over", { exact: true }).fill("5:30");
    await page.getByRole("button", { name: "Start period", exact: true }).click();
    await expect(page.getByText("Expected: 150:00 | Carry-over: 5:30", { exact: true })).toBeVisible();
    await page.reload();
    await expect(page.getByText("Expected: 150:00 | Carry-over: -3:00", { exact: true })).toBeVisible();
    await expect(page.getByText("Expected: 150:00 | Carry-over: 5:30", { exact: true })).toBeVisible();
});

test("existing planning data is preserved and upgraded when a session is saved", async ({ page }) => {
    const key = "flexisheet.workspace.v1";
    await page.addInitScript(({ key }) => {
        const expectedTime = Array.from({ length: 28 }, (_, index) => index % 7 < 5 ? "07:30:00" : "00:00:00");
        if (localStorage.getItem(key) === null) {
            localStorage.setItem(key, JSON.stringify({
                schemaVersion: 1,
                workingPattern: expectedTime,
                periods: [{
                    id: "00000000-0000-0000-0000-000000000001",
                    startDate: "2026-11-02",
                    expectedTime,
                    carryOver: "02:00:00"
                }]
            }));
        }
    }, { key });
    await page.goto("./");
    await page.getByRole("button", { name: "2 Nov 2026 to 29 Nov 2026" }).click();
    await recordSession(page, "09:00", "10:00");
    await expect(page.getByTestId("period-balance")).toContainText("balance -147:00");
    const stored = await page.evaluate(key => JSON.parse(localStorage.getItem(key)), key);
    expect(stored.schemaVersion).toBe(2);
    expect(stored.periods[0].id).toBe("00000000-0000-0000-0000-000000000001");
    expect(stored.periods[0].expectedTime[0]).toBe("07:30:00");
    expect(stored.periods[0].carryOver).toBe("02:00:00");
    expect(stored.periods[0].sessions).toHaveLength(1);
    await page.reload();
    await expect(page.getByText("Worked: 1:00 | Balance: -147:00", { exact: true })).toBeVisible();
});
