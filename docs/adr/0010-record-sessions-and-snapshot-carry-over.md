# 0010. Record non-overlapping sessions and snapshot carry-over

- Status: Accepted
- Date: 2026-11-12

## Context

[ADR-0007](0007-flexi-time-domain-model.md) left expected working time, midnight
boundaries, overlaps and carry-over behaviour open. The user has now confirmed
these rules, including how later periods behave when earlier records are edited.

## Decision

- A settings page defines expected working time for each day of a normal four-week
  period. New periods copy that pattern; individual period days can then be adjusted.
  Changing the normal pattern does not rewrite existing periods.
- Work sessions on a day must not overlap. Sessions sharing only an end/start instant
  are adjacent and valid. There is no arbitrary limit on session count.
- A session never crosses midnight. Work continuing after midnight is a new session
  on the next date. The UI accepts `24:00` as the first day's end-of-day boundary.
  Domain times remain `TimeOnly`, with an explicit `EndsAtMidnight` flag requiring
  an end time of `00:00`; this distinguishes the following boundary from the start
  of the same day. Durations use `TimeSpan`, with whole-minute precision.
- Day and Monday-to-Sunday week totals are derived from sessions. The period balance
  is its stored carry-over plus worked time minus expected time for all 28 days.
- When available, the immediately preceding period's balance can be copied into a
  new period. This is a snapshot of the records at creation, not a forecast.
- Carry-over can instead be entered manually when no immediately preceding period
  exists, or as an override. No business cap is applied.
- Editing or deleting work, adjusting expected time or changing carry-over in an
  earlier period never changes the carry-over stored in an already-created later
  period. The user must edit that later period to change its starting balance.

## Consequences

- Totals and balances cannot drift from their underlying sessions and expectations.
- Retrospective corrections do not silently rewrite later starting balances.
- Starting a later period before recording all earlier work may produce a provisional
  starting balance; the UI explains that it is fixed until manually changed.
- Stored JSON advances to schema version 2 to include sessions. Version 1 planning
  data is validated and read without losing its working pattern, expectations,
  dates, identifiers or carry-over, and is written as version 2 on the next save.
  Invalid, unsupported or incomplete documents are never replaced with empty data.
