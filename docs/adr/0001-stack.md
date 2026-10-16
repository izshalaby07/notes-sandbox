# ADR 0001: Technology stack

Status: Accepted (2026-10-16)

## Decision

| Part | Choice |
| --- | --- |
| Backend | .NET 10, ASP.NET Core minimal APIs |
| Tests | xUnit + `Microsoft.AspNetCore.Mvc.Testing` |
| Data | Decided in spec 001 (in-memory or SQLite) |
| Frontend | None |
| Hosting | None, runs locally and in CI only |

## Why

- Same platform as the owner's real projects, so the owner can judge the agents' code.
- Minimal APIs keep tickets small, which suits testing the agent pipeline.
- No frontend or hosting: the point is to test the team, not to ship an app.

## Consequences

- Everything is tested over HTTP; agents don't need a database server.
