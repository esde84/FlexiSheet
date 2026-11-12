import { expect } from "@playwright/test";

export async function savePattern(page) {
    await page.goto("settings");
    await expect(page.getByRole("heading", { name: "Normal four-week working pattern" })).toBeVisible();
    for (let week = 0; week < 4; week++) {
        for (let day = 0; day < 5; day++) {
            await page.locator(`#pattern-${week * 7 + day}`).fill("7:30");
        }
    }
    await page.getByRole("button", { name: "Save working pattern" }).click();
    await expect(page.getByRole("status").filter({ hasText: "Your normal working pattern has been saved" })).toBeVisible();
}

export async function startFirstPeriod(page, carryOver = "0:00") {
    await savePattern(page);
    await page.getByRole("link", { name: "Return to flexi periods" }).click();
    await page.getByLabel("Starting Monday").fill("2026-11-02");
    await page.getByLabel("Carry-over", { exact: true }).fill(carryOver);
    await page.getByRole("button", { name: "Start period", exact: true }).click();
    await expect(page.getByRole("heading", { name: "Work sessions and balances" })).toBeVisible();
}

export async function recordSession(page, start, end) {
    await page.getByLabel("Start time", { exact: true }).fill(start);
    await page.getByLabel("End time", { exact: true }).fill(end);
    await page.getByRole("button", { name: "Record session", exact: true }).click();
}
