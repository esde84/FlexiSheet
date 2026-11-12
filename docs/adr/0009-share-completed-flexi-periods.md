# 0009. Download and email completed flexi periods

- Status: Accepted
- Date: 2026-11-12

## Context

Users may need to send a completed flexi period to their manager or someone else. There is no server to send email, so any email must be created by the user's own email client.

## Decision

- A **complete** flexi period (one whose end date has passed) can be downloaded on its own as a JSON file.
- A complete flexi period offers an **email** option that opens a `mailto:` link with a pre-filled subject (for example `Flexi sheet for period starting 2026-11-02`) and a short body summarising the period dates, total time, carry over and balance.

## Consequences

- The `mailto:` scheme cannot attach files. The email option will therefore download the flexi period file first and the pre-filled email body will ask the user to attach the downloaded file before sending.
- The email is sent from the user's own email client and account; FlexiSheet never sees the recipient or the content.
- `mailto:` links have practical length limits, so the body is kept to a short summary rather than the full data.
