# 0011. Download any flexi period

- Status: Accepted
- Date: 2026-11-15

## Context

ADR-0009 made an individual JSON download available only for completed periods. Users also
need to download an in-progress period, while the email option is still intended for periods
that have ended.

## Decision

- Any saved flexi period can be downloaded on its own as a JSON file, whether its end date
  has passed or not.
- The email option is available only on the day after a period's end date, in local time.
- Email continues to download the period file and open a `mailto:` draft with a short summary.

## Consequences

- Users can export an in-progress period without waiting for it to end.
- Downloading a period does not make the email option available before the period's end date.
- ADR-0009 is superseded; its completed-period email behavior remains in effect.
