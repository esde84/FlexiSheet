# 0002. Host as a static site on GitHub Pages

- Status: Accepted
- Date: 2026-11-12

## Context

FlexiSheet is a personal tool for recording flexible working hours. It needs to be cheap and simple to host, and must not require any server infrastructure to operate or maintain.

## Decision

The application will be built as a static site and deployed to GitHub Pages, using a GitHub Actions workflow that builds and publishes the site.

## Consequences

- There is no back-end server, API or database; all logic runs in the browser (see [ADR-0003](0003-build-with-blazor-webassembly.md)) and all data is stored in the browser (see [ADR-0006](0006-store-data-in-the-browser-with-json-download.md)).
- Hosting is free and requires no operational support.
- Any scheduled behaviour, such as automatic deletion of old records, must happen when the user opens the application rather than on a server schedule.
- GitHub Pages serves the site from a sub-path (`/FlexiSheet/`), so the base path must be configured at build time, and client-side routing needs a `404.html` fallback.
- GitHub Pages runs Jekyll by default, which ignores folders starting with `_` (such as `_framework`); a `.nojekyll` file must be published.
