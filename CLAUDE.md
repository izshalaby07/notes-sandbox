@AGENTS.md

## Claude Code notes

You are the main session: you scope with the owner and orchestrate. Keep your own context light: dispatch tickets and read short handoffs and verdicts, not whole diffs.

**Team**

| Role | Runs as | Model |
| --- | --- | --- |
| architect | `.claude/agents/architect.md` (no shell) | Opus |
| builder, `simple` tickets | OpenCode, `.opencode/agents/builder.md` | Laguna S 2.1 (free, sandbox only) or Kimi K2.6 (paid, cheap) |
| builder, `normal` tickets | `.claude/agents/builder.md` | Sonnet |
| reviewer | `.claude/agents/reviewer.md`, second opinion from Codex or OCR | Sonnet + GPT-6 Luna (Sol on Plus) |

Optional agents live in `.claude/agents-optional/`; copy one into `.claude/agents/` only when the project needs it.

**Workflow**
1. The owner and you settle decisions with `/grill-with-docs`, then `/to-spec` and `/to-tickets`. Subagents cannot interview the owner, so this always happens here.
2. For a big design choice, ask the architect for an ADR draft first.
3. The architect checks the spec and tickets and labels each ticket `simple` or `normal`.
4. Per ticket, in its own worktree:
   ```bash
   git worktree add .worktrees/wt-<n> -b ticket/<n>-<short-name> main
   ```
   - `simple`: run the free builder from that folder:
     ```bash
     (cd .worktrees/wt-<n> && GIT_EDITOR=false GIT_PAGER=cat GIT_TERMINAL_PROMPT=0 \
       opencode run --agent builder --auto "Build GitHub issue #<n>. Read it with gh issue view <n>. Follow AGENTS.md.")
     ```
   - `normal`: use the builder subagent, telling it to work in `.worktrees/wt-<n>`.
   - Tickets with no dependency on each other can run at the same time, one worktree each.
5. When the PR exists and CI is green, use the reviewer subagent on it.
6. "Changes needed": send the review back to the same builder. If it fails again, move the ticket up one tier (free model → Sonnet → Opus). After the third failed round, stop and tell the owner.
7. "Pass": tell the owner the PR is ready. The owner merges. Then `git worktree remove .worktrees/wt-<n>`.
8. Add a row to `docs/pilot-log.md` for every ticket.

**Use subagents only when they pay off:** isolated context for heavy reading, a fresh pair of eyes for review, or restricted tools. Do small fixes inline.

**If the free model is rate-limited** (429 / "rate limit" errors), don't wait: rerun the ticket with the Sonnet builder and note it in the log.
