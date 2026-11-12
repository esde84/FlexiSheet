# Architecture Decision Records

This directory captures the significant architectural and product decisions made for FlexiSheet, using lightweight [Architecture Decision Records](https://adr.github.io/) (ADRs).

## How to add a decision

1. Copy [`template.md`](template.md) to a new file named `NNNN-short-title.md`, using the next number in sequence.
2. Fill in the context, decision and consequences.
3. Add the record to the index below.
4. Decisions are never deleted. If a decision changes, write a new ADR and mark the old one as `Superseded by [ADR-NNNN](NNNN-....md)`.

## Index

| ADR | Title | Status |
| --- | ----- | ------ |
| [0001](0001-record-architecture-decisions.md) | Record architecture decisions | Accepted |
| [0002](0002-host-as-static-site-on-github-pages.md) | Host as a static site on GitHub Pages | Accepted |
| [0003](0003-build-with-blazor-webassembly.md) | Build with C# .NET Blazor WebAssembly | Accepted |
| [0004](0004-clean-architecture-with-vertical-slices.md) | Clean architecture with vertical slices | Accepted |
| [0005](0005-use-gds-design-system-without-crown-branding.md) | Use the GOV.UK Design System without Crown branding | Accepted |
| [0006](0006-store-data-in-the-browser-with-json-download.md) | Store data in the browser with JSON download | Accepted |
| [0007](0007-flexi-time-domain-model.md) | Flexi time domain model | Accepted |
| [0008](0008-retention-backup-and-restore.md) | Two year retention, backup and restore | Accepted |
| [0009](0009-share-completed-flexi-periods.md) | Download and email completed flexi periods | Accepted |
| [0010](0010-record-sessions-and-snapshot-carry-over.md) | Record non-overlapping sessions and snapshot carry-over | Accepted |
