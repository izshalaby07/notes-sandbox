# Agent log

One row per ticket. Every ten tickets, look back: which model passed first time, where reviews caught real problems, what cost the most. Change the lineup from these numbers, not from opinions.

| Ticket | Size | Builder (model) | Reviewer (model) | Review rounds | CI first try? | Problems reviewer caught | Problems you caught | Time | Tokens / cost | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| #5 001-1 Health + fixture | simple | OpenCode, Laguna S 2.1 (free) | Sonnet; Codex did not run (sandbox blocked git) | 1 (Pass) | Yes | 0 blocking, 4 non-blocking | PR title from branch name, no `Closes #5` (fixed by hand) | 9.8 min (36 steps, 52 tool calls) | 1,262,386 tokens (202,930 new input + 1,041,440 cache read (82%) + 6,264 output + 11,752 reasoning) / $0 | First run failed: builder `mode: subagent` made OpenCode fall back to a model without tools (fixed in #13). Codex fix in #17. |
