# 0003. Build with C# .NET Blazor WebAssembly

- Status: Accepted
- Date: 2026-11-12

## Context

The application must run entirely in the browser as a static site ([ADR-0002](0002-host-as-static-site-on-github-pages.md)). The preferred language and ecosystem is C# and .NET.

## Decision

The application will be built with C# using .NET and Blazor WebAssembly in standalone mode (no ASP.NET Core host), using the current Long Term Support (LTS) version of .NET.

## Consequences

- Domain and application logic can be written and unit tested in C#.
- The published output (`wwwroot`) is a set of static files suitable for GitHub Pages.
- The initial download is larger than a typical JavaScript site because the .NET runtime is downloaded; this is acceptable for this tool and can be reduced with trimming and compression.
- Browser APIs that are not exposed by Blazor (for example `localStorage`, file download and file upload) will be accessed through small JavaScript interop wrappers, kept behind interfaces (see [ADR-0004](0004-clean-architecture-with-vertical-slices.md)).
