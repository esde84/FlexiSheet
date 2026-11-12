# FlexiSheet
A project to learn how to use CoPilot to create an interactive web app for recording flexi

## Record flexi time

The standalone .NET 10 LTS Blazor WebAssembly app now supports:

- Configuring expected working time for each of the 28 days of a normal four-week period.
- Starting a non-overlapping 28-day period on a Monday, copying the saved working pattern.
- Adjusting each period's expected daily time without changing the normal pattern or other periods.
- Recording, editing and deleting work sessions, rejecting overlaps while allowing adjacent sessions.
- Derived daily, Monday-to-Sunday weekly and four-week totals and balances.
- Copying the immediately preceding period's balance as a fixed carry-over snapshot, or entering a manual override without a business cap.
- Keeping versioned JSON in browser `localStorage` and requesting persistent storage where supported.
- Purging periods on app startup when their end date is strictly more than two calendar years ago.
- Downloading backups and restoring validated JSON after explicit replacement confirmation.
- Downloading completed periods individually and opening an email draft after downloading the period file.

Enter durations as `hours:minutes`, for example `7:30`, `0:00` or `-2:15` for carry-over.
Daily expected time is between `0:00` and `24:00`, in whole minutes. The initial pattern is
all zeroes: no contracted hours are assumed. Saving a new normal pattern affects only periods
created afterwards.

Session times use a 24-hour clock, in whole minutes. Sessions do not cross midnight:
finish at `24:00` on the first day and create a new session starting at `00:00` on the next.
There is no arbitrary limit on sessions per day.

The period balance includes expected time for all 28 days, not only days with recorded work.
When creating a period, the previous period's current balance can be copied automatically,
but subsequent changes to earlier work, expectations or carry-over **never cascade** into
already-created later periods. Edit a later period's carry-over explicitly to correct it.
If there is no immediately preceding period, enter your starting balance manually.

**Back up regularly:** browser data can be cleared or evicted.
Invalid or unsupported saved data blocks editing
without overwriting it; storage failures are shown rather than reported as successful saves.
Stale edits from another tab are rejected and require a page reload.
Schema version 1 planning data remains readable and is saved as version 2, including sessions,
on its next update. Invalid saved records are preserved rather than silently discarded.

## Backups, restore and sharing

Use **Backup and restore** to download a JSON file containing the normal working pattern
and all retained periods. Keep that file somewhere safe; the app does not schedule backups
or send data to a server.

To restore, select a JSON file and review the retained/expired counts before choosing
**Replace existing data**. This replaces all periods and the working pattern, rather than
merging. Invalid or unsupported files and cancelled restores leave existing data untouched.
Periods ending strictly more than two calendar years ago are excluded, including on restore.
Changing data in another tab invalidates a pending confirmation instead of silently
overwriting the newer data. A confirmed restore can also recover unreadable browser data;
the original document is not changed just by opening the app or inspecting a file.

A period becomes shareable on the day **after** its end date, in local time.
**Download JSON** exports only that period. **Email** first downloads the same file, then
requests a `mailto:` draft containing the dates, worked/expected time, carry-over and balance.
The draft asks you to attach the downloaded JSON file manually. The app cannot attach files,
detect whether an email client opened, or send the message. If no email app opens, compose
the message in your usual app and attach the downloaded file.

Both backup and individual-period files use the same validated JSON envelope:
`schemaVersion`, `workingPattern`, and `periods`. Current exports use version 2; version 1
planning files remain readable. Individual-period files have one period and a null
`workingPattern`. Restoring one therefore replaces all records with that period and removes
the normal pattern; the confirmation explains this before proceeding.

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
is distributed with the static app as `govuk-frontend-LICENSE.txt` and retains its
original third-party copyright notice. FlexiSheet is copyright (c) 2026 Scott Dawson;
its own MIT licence is distributed separately as `LICENSE.txt`.

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
Recording tests also cover adjacent/overlapping sessions, edits and confirmed deletion,
midnight boundaries, fixed automatic carry-over with manual overrides, and schema migration.
File tests inspect actual downloaded JSON, validate confirmation/cancellation and retention,
exercise corrupt-storage recovery and failed/stale writes, and verify download-before-email
ordering with no attachment query parameter. Browser email tests replace only the external
email-client launch; they do not send real messages.

`scripts/prepare-pages.mjs` sets the published base path (default `/FlexiSheet/`), adds
`404.html` for client-side routing and `.nojekyll` for framework assets. Set `BASE_PATH`
to another absolute path ending in `/` when publishing somewhere else; the browser test
server deliberately uses `/FlexiSheet/`. The build workflow runs both suites on pull
requests and deploys passing `main` builds to GitHub Pages. A repository administrator
must select **GitHub Actions** as the Pages source before the first deployment.
No deployment has been performed by adding this workflow.

## Structure

- `src/FlexiSheet.Domain`: framework-free session, day, week, working-pattern and period rules using `DateOnly`, `TimeOnly` and `TimeSpan`.
- `src/FlexiSheet.Application`: vertical slices for period/session management, backup preparation/restore, downloads and email; repository, clock, file-codec and delivery abstractions.
- `src/FlexiSheet.Infrastructure`: validated, source-generated JSON serialization, browser storage and file/email delivery.
- `src/FlexiSheet.Web`: form pages, accessible GOV.UK styles and dependency-injection composition.
- `tests`: focused C# tests and published-app Playwright tests.

The clarified recording and carry-over rules are recorded in [ADR-0010](docs/adr/0010-record-sessions-and-snapshot-carry-over.md).

## Decisions

Architecture and product decisions are recorded in [`docs/adr`](docs/adr/README.md).
