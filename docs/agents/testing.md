# Testing conventions

Read before writing or changing tests.

## What must have a test

- Every acceptance criterion in the ticket's spec.
- Every validation rule, both sides of the limit (e.g. title of 200 chars passes, 201 fails).
- Every error response (400, 404) the ticket introduces.

## How

| Do | Instead of |
| --- | --- |
| HTTP tests with `WebApplicationFactory<Program>` and `HttpClient` | calling endpoint methods directly |
| A fresh factory per test class (`IClassFixture`) and no shared state between tests | tests that depend on order |
| One behaviour per test, named `Action_Condition_Result` | long tests checking many things |
| A small hand-written `TimeProvider` stub for time; no extra packages | asserting on the real clock |
| Include at least one Arabic string (e.g. `"ملاحظة"`) in text tests | ASCII-only test data |

If `WebApplicationFactory<Program>` can't see `Program`, add `public partial class Program;` at the end of `Program.cs`.

## Never

- Never delete, skip or loosen a failing test to get green: fix the code, or stop and report.
