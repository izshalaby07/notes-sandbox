# Spec 001: Notes CRUD and search

Status: Approved (2026-10-16). Decisions settled in the 2026-10-16 grill session; next step is `/to-tickets`.

## Objective

A client can create, read, list, update, delete and search notes over HTTP, with Arabic and English text kept exactly as sent.

## Problem Statement

The sandbox has only the template's sample `weatherforecast` endpoint. There is nothing real for the agent team to build, test and review. A client has no way to save a note, read it back, find it again or remove it, and no way to be sure that Arabic text survives a round trip unchanged.

## Solution

A small HTTP API for notes. Clients create a note with a title and an optional body, read it by id, list notes newest first one page at a time, search titles and bodies for a piece of text, replace a note's title and body, and delete it permanently. All text, Arabic or English, is stored and returned exactly as sent. Bad input gets a 400 that names the field, and a missing note gets a 404. Both come back as `ProblemDetails`, never a raw exception. A `GET /health` endpoint replaces the sample endpoint.

## In scope

1. Replace the template's sample `weatherforecast` endpoint with `GET /health` returning `200 {"status":"ok"}`, and set up the HTTP test fixture.
2. `POST /notes`: create a note (title required, body optional). Returns 201 with the note and a `Location` header.
3. `GET /notes/{id}`: one note, or 404.
4. `GET /notes`: list notes, newest first, with paging.
5. `PUT /notes/{id}`: replace title and body; updates `updatedAt`. 404 if missing.
6. `DELETE /notes/{id}`: 204, or 404 if missing.
7. `GET /notes?search=<text>`: the list endpoint filtered to notes whose title or body contains the search text.

A note has: `id` (Guid), `title`, `body`, `createdAt`, `updatedAt` (UTC).

## User Stories

1. As a client, I want to create a note with a title and an optional body, so that I can save a piece of text.
2. As a client, I want invalid input rejected with a 400 that names the field, so that I know what to fix.
3. As an Arabic-speaking client, I want my text stored and returned exactly as I sent it, so that it is never altered.
4. As a client, I want to read one note by its id, and get a 404 when it doesn't exist, so that I can tell missing apart from empty.
5. As a client, I want to list notes newest first, one page at a time, so that I can browse them without loading everything.
6. As a client, I want to search titles and bodies in English or Arabic, so that I can find a note without knowing its id.
7. As a client, I want to replace a note's title and body, so that I can edit it.
8. As a client, I want the server to own ids and timestamps, so that they are unique and trustworthy.
9. As a client, I want to delete a note permanently, so that it is gone.
10. As an operator, I want a health endpoint, so that I can check the API is running.

## Implementation Decisions

- **Storage** (ADR-0002): one in-memory note store, a thread-safe singleton behind `INoteStore`. Notes are lost on restart. No new packages.
- **Feature layout**: a `Notes` feature with one `NotesEndpoints` class that maps a `/notes` group, plus a `Health` endpoint. Endpoints stay thin: validate, call the note store, map the result to request/response records. They never return the stored entity directly (see `docs/agents/backend.md`).
- **Note store interface**: add, get by id, list one page (with optional search text) returning the items and the total count, replace, and delete. Each returns whether the note was found, so the endpoints can choose between 200/204 and 404.
- **Ids**: server-generated `Guid`. Routes use a `guid` route constraint, so a malformed id returns 404.
- **Time**: `createdAt` and `updatedAt` are `DateTimeOffset` in UTC, taken from the injected `TimeProvider`. On create, `updatedAt` equals `createdAt`. Every successful `PUT` sets `updatedAt` to now, even when nothing changed.
- **Validation** (400 `ValidationProblem` naming the field):
  - `title`: required; `string.IsNullOrWhiteSpace` counts as missing; max 200.
  - `body`: optional; max 10,000.
  - Length is `string.Length` (UTF-16 code units). Arabic diacritics count as separate characters.
  - `page` ≥ 1; `pageSize` from 1 to 100. Values outside these ranges are rejected, not adjusted.
- **Text is never changed**: no trimming, no Unicode normalization, no case changes on what is stored or returned.
- **Request bodies**: `POST` and `PUT` take `title` and `body`. Any `id`, `createdAt` or `updatedAt` in the request is ignored. A missing or `null` `body` is stored as an empty string. Responses always return `body` as a string.
- **Responses**:
  - Note: `{ id, title, body, createdAt, updatedAt }`.
  - List and search: `{ items: [note...], page, pageSize, totalCount }`. A page past the end returns 200 with empty `items` and the real `totalCount`.
  - `POST` returns 201 with the note and `Location: /notes/{id}`.
  - `DELETE` returns 204 with no body.
  - Every 400 and 404 is `ProblemDetails`. No raw exceptions reach the client.
- **Ordering**: `createdAt` descending, then `id` descending when timestamps are equal.
- **Search**: `search` is an optional query parameter on `GET /notes`. A note matches if its title or body contains the search text, compared ordinal ignore-case. No Arabic normalization: `اولى` does not match `أولى`, and `ملاحظة` does not match `مُلاحَظة`. Search results use the same paging, ordering and envelope as the plain list. An empty or whitespace-only `search` counts as no filter.
- **Delete**: hard delete. The note is removed from the store at once.
- **Health**: `GET /health` returns `200 {"status":"ok"}`. The `weatherforecast` sample and its record are removed.

## Testing Decisions

- **One seam: HTTP.** All tests go through `WebApplicationFactory<Program>` and `HttpClient`, as in `docs/agents/testing.md`. The note store is an internal detail. Tests never call it, mock it or replace it. Add `public partial class Program;` if the factory can't see `Program`.
- **Time**: tests replace `TimeProvider` in the factory's services with a hand-written stub subclass that the test can set and advance. Don't add `Microsoft.Extensions.TimeProvider.Testing`: it's an extra package (an "Ask first").
- **Isolation**: each factory has its own store. Tests that assert on the whole list (ordering, paging, `totalCount`, search counts) create their own factory in the test, so they start from an empty store. Other tests can share a class fixture but must not depend on what other tests created.
- **What a good test checks**: one behaviour, named `Action_Condition_Result`, checking only the status code, headers and JSON. Both sides of every limit are tested (200/201 and 10,000/10,001). Every 400 and 404 is tested. Text tests include at least one Arabic string. Test data is invented, never real personal data.
- **Prior art**: none yet. The only test is the template's empty `UnitTest1`, which ticket 1 replaces with the first health test.

## Acceptance criteria

- [ ] `GET /health` returns 200 and `{"status":"ok"}`; the sample endpoint is gone.
- [ ] Creating a note with an empty, whitespace-only or missing title returns 400 `ValidationProblem` naming the `title` field.
- [ ] A 201-character title returns 400; exactly 200 succeeds. A 10,001-character body returns 400 naming `body`; exactly 10,000 succeeds.
- [ ] `POST` returns 201, the note, and a `Location` header that reads the note back with `GET`.
- [ ] A note created with an Arabic title (e.g. `"ملاحظة أولى"`) reads back byte-for-byte identical; leading and trailing spaces are kept.
- [ ] A note created without a body returns `body` as `""`, not `null`.
- [ ] Unknown id on GET, PUT or DELETE returns 404 `ProblemDetails`; a malformed id returns 404.
- [ ] `PUT` changes `updatedAt` but not `createdAt` (tested with a stub `TimeProvider`); a `PUT` without a body clears the body; client-sent `id`, `createdAt` and `updatedAt` are ignored.
- [ ] `PUT` with an invalid title or body returns 400 and leaves the note unchanged.
- [ ] `DELETE` returns 204; after it, GET, PUT and a second DELETE on that id return 404.
- [ ] List returns `{ items, page, pageSize, totalCount }`, newest `createdAt` first, with ties broken by `id`, and respects the page size; asking past the last page returns 200 with empty `items`.
- [ ] `GET /notes` with no paging parameters uses page 1 and page size 20.
- [ ] `page=0`, `pageSize=0` and `pageSize=101` each return 400 naming the field; `pageSize=100` succeeds.
- [ ] Search matches regardless of case for English and finds Arabic text inside a longer body; results are paged and ordered like the list; an empty `search` returns the unfiltered list.

## Out of scope (later specs)

- Users, login, sharing.
- Tags, attachments, rich text.
- Any frontend.
- Persistent storage (would replace ADR-0002).
- Arabic search normalization (alef variants, diacritics, tatweel).
- Soft delete, undo or trash.
- Partial updates (`PATCH`).

## Boundary checks

- No packages beyond `Microsoft.AspNetCore.Mvc.Testing`. In-memory storage needs none, and time is faked with a stub, not `Microsoft.Extensions.TimeProvider.Testing`.
- No real personal data in tests: use invented note text.
- Errors go out as `ProblemDetails` / `ValidationProblem`, never as raw exceptions.

## Further Notes

- Ordinal ignore-case search was chosen to keep the AGENTS.md rule ("text kept exactly as sent"). It finds Arabic only when the characters match exactly, which is accepted for this spec.
- Because lengths are counted in UTF-16 code units, an Arabic title with diacritics holds slightly fewer visible letters than 200. This is accepted.
