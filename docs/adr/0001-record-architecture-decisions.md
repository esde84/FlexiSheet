# 0001. Record architecture decisions

- Status: Accepted
- Date: 2026-11-12

## Context

FlexiSheet needs a place to record the architectural and product decisions that shape it, so that the reasoning behind them is not lost and can be revisited later.

## Decision

We will use Architecture Decision Records (ADRs), stored as Markdown files in `docs/adr`, following the format in [`template.md`](template.md). Each record is numbered sequentially and listed in the [index](README.md).

## Consequences

- Decisions and their rationale are versioned alongside the code and reviewed through pull requests.
- Records are immutable once accepted; changing a decision requires a new ADR that supersedes the old one.
