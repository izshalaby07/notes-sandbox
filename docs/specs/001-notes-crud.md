# Spec 001: Notes CRUD and search

Status: Draft. Resolve open questions with `/grill-with-docs` before `/to-tickets`.

## Objective

A client can create, read, list, update, delete and search notes over HTTP, with Arabic and English text kept exactly as sent.

## In scope

1. Replace the template's sample `weatherforecast` endpoint with `GET /health` returning `200 {"status":"ok"}`, and set up the HTTP test fixture.
2. `POST /notes`: create a note (title required, body optional). Returns 201 with the note and a `Location` header.
3. `GET /notes/{id}`: one note, or 404.
4. `GET /notes`: list notes, newest first, with paging.
5. `PUT /notes/{id}`: replace title and body; updates `updatedAt`. 404 if missing.
6. `DELETE /notes/{id}`: 204, or 404 if missing.
7. `GET /notes?search=<text>`: notes whose title or body contains the text, case-insensitive, works for Arabic.

A note has: `id` (Guid), `title`, `body`, `createdAt`, `updatedAt` (UTC).

## Out of scope (later specs)

- Users, login, sharing.
- Tags, attachments, rich text.
- Any frontend.

## Acceptance criteria

- [ ] `GET /health` returns 200 and `{"status":"ok"}`; the sample endpoint is gone.
- [ ] Creating a note with an empty or missing title returns 400 `ValidationProblem` naming the `title` field.
- [ ] Title over the maximum length returns 400; exactly the maximum succeeds.
- [ ] A note created with an Arabic title (e.g. `"ملاحظة أولى"`) reads back byte-for-byte identical.
- [ ] Unknown id on GET, PUT or DELETE returns 404 `ProblemDetails`.
- [ ] `PUT` changes `updatedAt` but not `createdAt` (tested with a fake `TimeProvider`).
- [ ] List returns newest first and respects the page size; asking past the last page returns an empty list, not an error.
- [ ] Search matches regardless of case for English and finds Arabic text inside a longer body.

## Boundary checks

- No packages beyond `Microsoft.AspNetCore.Mvc.Testing` unless the storage decision needs one (that is an "Ask first").
- No real personal data in tests: use invented note text.

## Open questions (for the grill session)

1. Storage: in-memory (lost on restart, simplest) or SQLite with EF Core (persists, adds packages and migrations)?
2. Title maximum length (200?) and body maximum length (10,000?).
3. Paging: `page`/`pageSize` or `offset`/`limit`? Default and maximum page size?
4. Delete: hard delete, or soft delete (hidden but kept)?
5. Search: separate `GET /notes/search?q=` or a `search` filter on `GET /notes`? Combine with paging?
