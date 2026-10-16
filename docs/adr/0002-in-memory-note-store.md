# ADR 0002: In-memory note store

Status: Accepted (2026-10-16)

ADR-0001 left storage to spec 001. We keep notes in memory, in a single thread-safe note store registered as a singleton behind an `INoteStore` interface, rather than SQLite with EF Core. This project exists to test the agent team, not to keep data, so losing notes on restart costs nothing. In-memory storage needs no extra packages, migrations or database file, and agents and CI need nothing beyond `dotnet test`.

## Consequences

- All notes are lost when the process stops. That is expected, not a bug.
- Endpoints talk only to `INoteStore`, so a persistent store can replace it later without changing the endpoints. That change would need a new ADR.
- Every `WebApplicationFactory` instance gets its own store. Tests that need an empty store create their own factory.
