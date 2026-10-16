# Notes Sandbox

A tiny notes API used to test the AI agent team. Throwaway code, no real data, Arabic and English text.

## Commands

Run the narrowest command that checks your change. Full build, test and format check before you push.

```
dotnet build                                            # build everything
dotnet test                                             # all tests
dotnet test --filter FullyQualifiedName~NotesEndpoints  # one test class
dotnet format                                           # fix formatting
dotnet format --verify-no-changes                       # what CI checks
dotnet run --project src/Notes.Api                      # run locally
```

## Stack

.NET 10 · ASP.NET Core minimal APIs · xUnit · `Microsoft.AspNetCore.Mvc.Testing` for HTTP-level tests.
Storage is decided in spec 001. Reasons are in `docs/adr/`.

## Layout

- `src/Notes.Api/`: the API. Endpoints are grouped per feature in `Features/<Feature>/`.
- `tests/Notes.Api.Tests/`: tests, mirroring the `src` folders.

## Boundaries

**Always**
- Write the failing test first, then the least code that passes it.
- Test through HTTP with `WebApplicationFactory<Program>` unless the logic is pure.
- Reuse what exists (this repo, then .NET built-ins) before writing new code.
- Use the terms in `docs/GLOSSARY.md`.
- Leave a handoff in the PR: what changed, files touched, tests run, open questions.

**Ask first**
- Adding any NuGet package other than `Microsoft.AspNetCore.Mvc.Testing`.
- Any change not in the active spec in `docs/specs/`.
- Editing `AGENTS.md`, `CLAUDE.md`, `docs/adr/`, `.github/`, `.claude/` or `.opencode/`.

**Never** (each with what to do instead)
- Never delete, skip or weaken a failing test: fix the code, or stop and report.
- Never push to `main` or merge: push your branch, open a PR and wait for review.
- Never commit secrets or keys: there are none in this project, keep it that way.
- Never return a raw exception to the client: return `ProblemDetails` (`Results.Problem` / `Results.ValidationProblem`).

## Decisions

| When you need to... | Use | Not |
| --- | --- | --- |
| An id | `Guid` (server-generated) | `int`, client-sent ids |
| A timestamp | `DateTimeOffset` in UTC, from an injected `TimeProvider` | `DateTime.Now` |
| Report bad input | `Results.ValidationProblem(...)` → 400 | throwing, or 500 |
| Report a missing note | 404 with `ProblemDetails` | 200 with null |
| Compare text in search | Ordinal ignore-case, text kept exactly as sent | changing the stored text |

## Read when relevant

- `docs/agents/backend.md`: before changing server code.
- `docs/agents/testing.md`: before writing or changing tests.
- `docs/specs/`: the spec for your ticket. Build only what it says.
- `docs/adr/`: before changing architecture. A change needs a new ADR, not an edit.

## Keeping this file useful

Only what an agent cannot infer from the code. When an agent repeats a mistake, propose one "do" rule and ask the owner.
