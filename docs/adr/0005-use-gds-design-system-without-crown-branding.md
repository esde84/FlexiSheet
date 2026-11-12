# 0005. Use the GOV.UK Design System without Crown branding

- Status: Accepted
- Date: 2026-11-12

## Context

The GOV.UK Design System (GDS) provides accessible, well tested styles, components and patterns that suit a simple form-based tool like FlexiSheet. The `govuk-frontend` code is open source under the MIT licence. However, FlexiSheet is **not** a government service, and the GOV.UK brand assets are restricted to use on GOV.UK services.

## Decision

The web user interface will use the GOV.UK Design System (`govuk-frontend`) styles, components and patterns, with the following restrictions:

- The Crown logo, the Royal Coat of Arms and the GOV.UK logotype will not be used anywhere, including the header, favicons and app icons.
- The header will show the FlexiSheet service name only and will not link to or claim to be GOV.UK.
- The GDS Transport typeface will not be used, as it is licensed only for GOV.UK services. A system font stack (for example Arial / Helvetica) will be used instead.
- The footer will not include Crown copyright.

## Consequences

- The application benefits from GDS accessibility and usability patterns.
- Brand assets that ship with `govuk-frontend` must be excluded or overridden in the build.
- Users should not mistake FlexiSheet for an official government service.
