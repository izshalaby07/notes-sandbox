# Agent log

One row per ticket. Every ten tickets, look back: which model passed first time, where reviews caught real problems, what cost the most. Change the lineup from these numbers, not from opinions.

| Ticket | Size | Builder (model) | Reviewer (model) | Review rounds | CI first try? | Problems reviewer caught | Problems you caught | Time | Tokens / cost | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| #5 001-1 Health + fixture | simple | OpenCode, Laguna S 2.1 (free) | Sonnet; Codex did not run (sandbox blocked git) | 1 (Pass) | Yes | 0 blocking, 4 non-blocking | PR title from branch name, no `Closes #5` (fixed by hand) | not measured | free / not measured | First run failed: builder `mode: subagent` made OpenCode fall back to a model without tools (fixed in #13). Codex fix in #17. |
