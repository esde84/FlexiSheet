# 0007. Flexi time domain model

- Status: Accepted
- Date: 2026-11-12

## Context

FlexiSheet records flexible working hours and calculates the flexi balance. The core concepts need to be defined consistently so that the domain model and calculations are unambiguous.

## Decision

The domain will be modelled with the following concepts:

- **Session** – a period of work with a start time and an end time. The duration of a session is `end - start`. A session's end time must be after its start time.
- **Day** – a calendar date holding zero or more sessions. There is no upper limit on the number of sessions in a day (more than 10 is valid). The total time for a day is the sum of its session durations.
- **Week** – seven days running Monday to Sunday. The total time for a week is the sum of its days.
- **Flexi period** – four consecutive weeks (28 days) starting on a defined Monday and ending on the Sunday 27 days later. A flexi period records a **carry over** balance from the previous flexi period, which may be positive or negative. The total time for a flexi period is the sum of its weeks.
- **Balance** – the carry over plus the time recorded in the flexi period, compared against the time expected to be worked in that period.

Durations are held as `TimeSpan` and dates as `DateOnly` / times as `TimeOnly`, in the user's local time.

## Consequences

- Totals are always derived from sessions rather than stored, so they cannot get out of step.
- A flexi period start date that is not a Monday is rejected.

## Open questions

These will be resolved in later ADRs as the use cases are built:

- How the expected working time is defined (for example contracted hours per day or per week) and whether it varies by day.
- Whether sessions may cross midnight, and whether overlapping sessions in a day are allowed.
- Whether carry over is entered manually or calculated automatically from the previous flexi period, and whether there is a maximum carry over.
