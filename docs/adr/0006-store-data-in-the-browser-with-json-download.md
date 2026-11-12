# 0006. Store data in the browser with JSON download

- Status: Accepted
- Date: 2026-11-12

## Context

There is no server ([ADR-0002](0002-host-as-static-site-on-github-pages.md)), so all data must be stored on the user's device. The data volume is small: two years of flexi periods (about 26 periods, around 730 days, each with a handful of sessions) is well under 1 MB of JSON.

Browser storage can be cleared by the user or the browser, so users need a way to take a copy of their data.

## Decision

- Application data will be stored in the browser's `localStorage`, serialised as JSON, accessed only through a repository interface defined in the Application layer and implemented in Infrastructure.
- The application will request persistent storage (`navigator.storage.persist()`) where supported, to reduce the chance of the browser evicting data.
- Data can be downloaded as a JSON file. Exported files include a `schemaVersion` so that future versions of the application can migrate older files on import.
- No data is sent to any server or third party.

## Consequences

- Data is tied to a single browser on a single device; moving devices requires export and import (see [ADR-0008](0008-retention-backup-and-restore.md)).
- Clearing browser data deletes the user's records unless they have a backup.
- Because storage is behind an interface, it can be changed to IndexedDB later without affecting the domain or use cases, if `localStorage` limits become a problem.
