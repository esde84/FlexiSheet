# 0004. Clean architecture with vertical slices

- Status: Accepted
- Date: 2026-11-12

## Context

The application should be easy to understand, change and test as it grows. Business rules (such as how sessions, days and flexi periods add up) must not be tangled with UI or browser storage details.

## Decision

The solution will follow clean architecture principles, with features organised as vertical slices around use cases.

- **Domain** – entities and value objects (for example `Session`, `Day`, `Week`, `FlexiPeriod`) and the rules for calculating totals and balances. No dependencies on other projects or frameworks.
- **Application** – one folder per use case (vertical slice), for example `RecordSession`, `StartFlexiPeriod`, `ExportBackup`, `ImportBackup`, `DownloadFlexiPeriod`, `PurgeExpiredFlexiPeriods`. Each slice contains its request, handler, validation and any DTOs it needs. Slices depend only on the domain and on abstractions (interfaces) such as a repository or a clock.
- **Infrastructure** – implementations of those abstractions, such as browser storage and JSON serialisation.
- **Web (Blazor UI)** – pages and components that call use cases. Pages may be grouped alongside the slice they present.

Dependencies always point inwards: Web → Application → Domain, with Infrastructure implementing Application abstractions and being wired up through dependency injection at start-up.

## Consequences

- Each use case can be developed and tested end to end in isolation.
- Domain and Application code can be unit tested without a browser.
- The current date is obtained through an abstraction (for example `TimeProvider`) so that date-based rules, such as retention, can be tested.
- Some duplication between slices is accepted in preference to premature shared abstractions.
