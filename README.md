# FlexiSheet
A project to learn how to use CoPilot to create an interactive web app for recording flexi

## First slice: plan a flexi period

The standalone .NET 10 LTS Blazor WebAssembly app now supports:

- Configuring expected working time for each of the 28 days of a normal four-week period.
- Starting a non-overlapping 28-day period on a Monday, copying the saved working pattern.
- Adjusting each period's expected daily time without changing the normal pattern or other periods.
- Entering and updating positive or negative carry-over manually, without a business cap.
- Keeping versioned JSON in browser `localStorage` and requesting persistent storage where supported.
- Purging periods on app startup when their end date is strictly more than two calendar years ago.

Enter durations as `hours:minutes`, for example `7:30`, `0:00` or `-2:15` for carry-over.
Daily expected time is between `0:00` and `24:00`, in whole minutes. The initial pattern is
all zeroes: no contracted hours are assumed. Saving a new normal pattern affects only periods
created afterwards.

This is a **period-planning slice**, not a complete time-recording tool yet. Session recording,
worked-time totals and balances, automatic carry-over, backup/restore, individual downloads
and email are not implemented. Carry-over must therefore be entered manually for now.
**Do not rely on this release as your only record:** browser data can be cleared or evicted,
and backup/restore is not available yet. Invalid or unsupported saved data blocks editing
without overwriting it; storage failures are shown rather than reported as successful saves.
Stale edits from another tab are rejected and require a page reload.

## Run locally

Install the .NET 10 SDK and Node.js 22 (or a compatible supported Node version).

```sh
npm ci
npm run build:css
dotnet run --project src/FlexiSheet.Web
```

Open the localhost URL printed by .NET. Rebuild CSS after changing the Sass source.
The generated CSS is committed so running or publishing with .NET alone also works.
Only the required GOV.UK Frontend styles are compiled, with system fonts and without
Crown logos, GDS Transport, font files, or other branding assets. Its MIT licence notice
is distributed with the static app; this does not claim Crown copyright over FlexiSheet.

## Validate and publish

```sh
dotnet test FlexiSheet.sln --configuration Release
dotnet publish src/FlexiSheet.Web --configuration Release --output artifacts/publish
node scripts/prepare-pages.mjs
npx playwright install chromium
npm run test:browser
```

The browser tests exercise the **published**, trimmed app at `/FlexiSheet/`, including
direct settings navigation, persistence across reloads, independent period adjustments,
validation, corrupt storage, failed writes and stale tabs.

`scripts/prepare-pages.mjs` sets the published base path (default `/FlexiSheet/`), adds
`404.html` for client-side routing and `.nojekyll` for framework assets. Set `BASE_PATH`
to another absolute path ending in `/` when publishing somewhere else; the browser test
server deliberately uses `/FlexiSheet/`. The build workflow runs both suites on pull
requests and deploys passing `main` builds to GitHub Pages. A repository administrator
must select **GitHub Actions** as the Pages source before the first deployment.
No deployment has been performed by adding this workflow.

## Structure and next decisions

- `src/FlexiSheet.Domain`: framework-free working-pattern and period rules using `DateOnly` and `TimeSpan`.
- `src/FlexiSheet.Application`: use-case folders for loading/purging, saving the working pattern, starting and adjusting periods; repository and clock abstractions.
- `src/FlexiSheet.Infrastructure`: validated, source-generated JSON serialization and browser storage.
- `src/FlexiSheet.Web`: form pages, accessible GOV.UK styles and dependency-injection composition.
- `tests`: focused C# tests and published-app Playwright tests.

The next recording slice needs an answer on **overlapping work sessions within the same day**:
should they be rejected, or allowed (and, if allowed, counted twice or as a union)?
Sessions will not cross midnight; continued work starts a new session at midnight on the next day.
An end-of-day boundary must therefore be represented as `24:00` on the first day, not
as an earlier end time. Another decision for automatic carry-over is whether changing an
earlier period should recalculate already-created later periods whose carry-over was
not manually overridden, or leave their starting balances fixed. Neither choice is
silently implemented in this slice.

## Decisions

Architecture and product decisions are recorded in [`docs/adr`](docs/adr/README.md).
