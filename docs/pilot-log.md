# Agent log

One row per ticket. Every ten tickets, look back: which model passed first time, where reviews caught real problems, what cost the most. Change the lineup from these numbers, not from opinions.

| Ticket | Size | Builder (model) | Reviewer (model) | Review rounds | CI first try? | Problems reviewer caught | Problems you caught | Time | Tokens / cost | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| #5 001-1 Health + fixture | simple | OpenCode, Laguna S 2.1 (free) | Sonnet; Codex did not run (sandbox blocked git) | 1 (Pass) | Yes | 0 blocking, 4 non-blocking | PR title from branch name, no `Closes #5` (fixed by hand) | 9.8 min (36 steps, 52 tool calls) | 1,262,386 tokens (202,930 new input + 1,041,440 cache read (82%) + 6,264 output + 11,752 reasoning) / $0 | First run failed: builder `mode: subagent` made OpenCode fall back to a model without tools (fixed in #13). Codex fix in #17. |
| #6 001-2a Create + read | normal | Claude Code subagent, Sonnet | Sonnet + Codex gpt-6-luna (read the diff) | 2 (Pass + 2 should-fix → fix round → Pass) | Yes (both rounds) | 0 blocking; weak timestamp test (fixed in round 2); expected #8/#9 merge conflicts. Codex: 5 found / 0 valid (all were later tickets' scope) | Verdict slip "#9 (update)" should be #10 (main session note) | Builder 99.9 s + fix 33.3 s; reviewer 67.5 s + 18.5 s | Builder 205,891 tokens (cache read 145,002 = 70%, cache write 60,757), ≈ $0.27+; reviewer 187,108 (66% cache read), ≈ $0.27+ | Token counts summed from the subagent transcripts; output tokens are under-logged (≤ 25 per call), so $ is a lower bound. First reviewer launch blocked by auto mode, which led to the verdict-file rule (#20). |
| #9 001-6 Delete | simple | OpenCode, Laguna S 2.1 (free), rate-limited → rerun on Claude Code subagent, Sonnet | Sonnet + Codex gpt-6-luna (read the diff) | 1 (Pass) | Yes | 0 blocking; 2 nice-to-have (assert 404 `title`; comment on malformed-id 404). Codex: 0 found / 0 valid | — | Laguna 4.2 min (13 steps, 31 tool calls) until `free-models-per-day`; Sonnet rerun 65.1 s; reviewer 50.8 s | Laguna 257,711 tokens (50% cache read), $0; Sonnet 84,250 (83% cache read), ≈ $0.07+; reviewer 56,832 (70%), ≈ $0.08+ | Laguna wrote `DeleteNoteTests.cs` before the limit; Sonnet kept it unchanged (5 tests failed before code, passed after). Ran in parallel with #7 on the free tier; both hit the daily limit (≈ 50 requests). Merged first. |
| #8 001-3 List + paging | normal | Claude Code subagent, Sonnet | Sonnet + Codex gpt-6-luna (read the diff) | 3 (Changes needed → fix → Pass → rebase onto #9 → Pass) | Yes (all pushes) | Must fix: `(page - 1) * pageSize` int overflow returned page 1 for huge `page`; reviewer proved the new test fails on the old code. Codex: 1 found / 1 valid (same overflow) | Builder admitted it never saw the fix test fail → asked reviewer to prove it in a temp worktree | Builder 139.4 s + fix 60.9 s + rebase 108.1 s; reviewer 69.2 s + 40.2 s + 36.9 s | Builder 577,161 tokens (86% cache read), ≈ $0.46+; reviewer 216,066 (69%), ≈ $0.30+ | Rebase conflicts in `INoteStore` / `InMemoryNoteStore` resolved by keeping both sides; range-diff confirmed #8's code unchanged. Output tokens under-logged, $ is a lower bound. |

## Session cost

**Night of 2026-10-16** (spec 001 setup + tickets #5, #6, #8, #9), from `npx ccusage@latest session`:

| Who | Model | Cost |
| --- | --- | --- |
| Main session (orchestrator) | Opus | ≈ $8.49 |
| All subagents (builders, reviewers) | Sonnet | ≈ $0.81 |

The orchestrator cost about 10x all the building and reviewing together.

Snapshot at 21:29 UTC; both are minimums; rerun ccusage at session end for final figures.

**Action:** from now on use Opus only for scoping (`/grill-with-docs`, `/to-spec`, `/to-tickets`), then `/model sonnet` for dispatching, posting and logging. Compare the next session's ccusage numbers.

## Setup issues

| # | Issue | Cause | Fix | Status |
| --- | --- | --- | --- | --- |
| 1 | Free builder never ran | `.opencode` builder had `mode: subagent`; `opencode run` fell back to an image model | `mode: all` + top-level `"model"` in `opencode.json` | Fixed |
| 2 | Session export unreadable by `ticket-stats` | PowerShell `>` writes UTF-16 | Script detects the encoding | Fixed |
| 3 | Codex second opinion saw no code | Windows read-only sandbox blocks `git` and file reads | Prompt, ticket, spec and diff sent on stdin; `Files reviewed:` check | Fixed |
| 4 | Codex 5 findings / 0 valid on #6 | Prompt had the whole spec but not the ticket | Ticket text + "review only this ticket's scope" | Fixed, watching |
| 5 | Reviewer launch / posting blocked | Auto-mode safety check | Reviewer writes a verdict file, main session posts it verbatim; owner approves launches when asked | Fixed |
| 6 | #7 and #9 stopped mid-ticket | Free tier ≈ 50 requests/day without credit; two Laguna runs in parallel | No parallel free-model runs; credit or fallback undecided | Open |
| 7 | Sonnet usage not comparable with OpenCode's | Subagent count shows final context; ccusage can't split subagents and undercounts output | Summed subagent logs; costs are minimums | Open (Phoenix later) |
| 8 | No live builder log | Run command didn't redirect output | Log file + `--title` in `CLAUDE.md` | Fixed |
| 9 | CI warnings | Node 20 actions; `ubuntu-latest` moves to Ubuntu 26 on Oct 19 | Actions bumped, Dependabot added; watch CI after the 19th | Fixed / watching |
| 10 | PR titles from branch names, no issue link | `gh pr create --fill` | Builders use the issue title + `Closes #n` | Fixed |
