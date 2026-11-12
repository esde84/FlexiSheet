# 0008. Two year retention, backup and restore

- Status: Accepted
- Date: 2026-11-12

## Context

Users need access to their recent flexi history, but data should not be kept indefinitely. Because data lives only in the browser ([ADR-0006](0006-store-data-in-the-browser-with-json-download.md)), users also need a way to back up and restore their records.

## Decision

- **Retention** – flexi periods for the last two years are retained. A flexi period whose end date is more than two years before the current date is deleted automatically.
- **When deletion runs** – as there is no server, the purge runs each time the application starts, and after a backup is imported (so that importing an old backup does not restore expired periods).
- **Backup** – all retained flexi periods can be downloaded together as a single JSON file.
- **Restore** – a backup file can be imported to restore the record. The file is validated (including its `schemaVersion`) before any existing data is changed; an invalid file leaves existing data untouched. The user is asked to confirm before existing data is replaced.

## Consequences

- If the application is not opened for a long time, expired periods remain stored until the next time it is opened. This is acceptable as the data never leaves the user's device.
- Users are responsible for keeping their own backups if they want to keep records for longer than two years.
- Backup and restore also provide the way to move data between browsers or devices.
