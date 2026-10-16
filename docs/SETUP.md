# Setup guide (Windows)

Follow the parts in order. Each step ends with a **Check**: don't move on until it passes.
Commands are for **PowerShell** unless marked otherwise.

- **Part A**: install the tools (once per computer, about 30 min)
- **Part B**: publish this template to GitHub (once)
- **Part C**: create the sandbox project and protect `main` (once)
- **Part D**: first test run
- **Part E**: when something goes wrong

---

## Part A: Install the tools

### A1. Basics

You likely have most of these. Install what's missing:

```powershell
winget install Git.Git
winget install OpenJS.NodeJS.LTS
winget install Microsoft.DotNet.SDK.10
winget install GitHub.cli
```

Close and reopen PowerShell, then sign in to GitHub:

```powershell
gh auth login
```

**Check:**
```powershell
git --version; node --version; dotnet --version; gh auth status
```
Node must be 20 or newer, .NET must be 10.x, and `gh` must say "Logged in".

### A2. Claude Code (main session and Claude agents)

Already installed. Make sure the plugins and skills from earlier are there:

```powershell
claude --version
```
In Claude Code, `/plugin` should list `agent-skills` and `context-mode`, and `/` should show Matt's skills (`grill-with-docs`, `to-spec`, `to-tickets`, `handoff`).

**Check:** `/grill-with-docs` appears when you type `/grill`.

### A3. OpenCode (runs non-Claude agents)

```powershell
npm install -g opencode-ai
```

**Check:** `opencode --version` prints a version.

### A4. OpenRouter key (free and cheap builder models)

1. Make an account at openrouter.ai, then go to **Keys** and create a key.
2. Give it to OpenCode:
   ```powershell
   opencode auth login --provider openrouter
   ```
   Paste the key when asked.
3. In OpenRouter **Settings → Privacy**, check what free endpoints need. Some free models only run if you allow the provider to log prompts. **This is why free models are for the sandbox and side projects only, never Palm Hills code.**

**Check:**
```powershell
opencode models openrouter --refresh | Select-String ":free"
```
Free models change every few weeks. The builder is set to `openrouter/poolside/laguna-s-2.1:free` (a coding-agent model; free use may train on your inputs; listed as ending Oct 31, 2026). If it's gone, pick another free coding model from the list, or use paid Kimi K2.6 (`openrouter/moonshotai/kimi-k2.6`, a few cents per ticket, needs $5–10 credit). Put the exact name in the `model:` line of `.opencode/agents/builder.md`.

Then a quick live test:
```powershell
opencode run --model openrouter/poolside/laguna-s-2.1:free "Reply with the word ready"
```

### A5. Codex CLI (the reviewer's second opinion, non-Claude)

```powershell
npm install -g @openai/codex
codex login
```
Choose **Sign in with ChatGPT** if you have a ChatGPT plan, or use an OpenAI API key.

**Check:**
```powershell
codex exec -s read-only -m gpt-6-luna "Reply with the word ready"
```
Allow the Windows sandbox when Codex asks. A free ChatGPT account gets `gpt-6-luna`; Plus and higher get stronger models. Run `codex`, then `/model` to see yours, and put the name in `.claude/agents/reviewer.md`. An "ERROR ... failed to refresh available models: request timed out" line is harmless if the reply still arrives.

> You already installed Open Code Review (OCR). Keep it as the backup. The reviewer uses Codex first, and OCR if Codex isn't there.

### A6. (Optional) FreeLLMAPI

Skip for the first test. Add it later if Kimi's free limits keep stopping you. It needs Docker Desktop. Install it from its GitHub page, open `http://localhost:3001`, add 2–3 free provider keys, and **pin a short chain of strong coding models (not `auto`)**. Then add it to `opencode.json` as a provider:

```json
"provider": {
  "freellmapi": {
    "npm": "@ai-sdk/openai-compatible",
    "name": "FreeLLMAPI",
    "options": { "baseURL": "http://localhost:3001/v1", "apiKey": "{env:FREELLMAPI_KEY}" },
    "models": { "auto": { "name": "Pinned free chain" } }
  }
}
```

---

## Part B: Publish this template to GitHub

Unzip `agent-team-template.zip` somewhere like `C:\dev\agent-team-template`.

**If you never pushed the template before:**
```powershell
cd C:\dev\agent-team-template
git init -b main
git add .
git commit -m "Agent team template v4"
gh repo create agent-team-template --private --source . --push
gh repo edit --template
```

**If you already pushed v3:** copy the v4 files over your clone (replace when asked), then:
```powershell
git add -A
git commit -m "Template v4: OpenCode agents, guardrails, CI"
git push
```

**Check:** on GitHub, the repo page shows a green **Use this template** button.

---

## Part C: Create the sandbox and protect `main`

The sandbox is a tiny Notes API. It has no real data, so free models are safe, and you can throw it away.

### C1. Create the repo

Unzip `notes-sandbox.zip` to `C:\dev\notes-sandbox`, then:

```powershell
cd C:\dev\notes-sandbox
git init -b main
```

### C2. Generate the .NET skeleton

These commands use Microsoft's own templates, so the starting code is known to build:

```powershell
dotnet new sln -n Notes
dotnet new webapi -n Notes.Api -o src/Notes.Api
dotnet new xunit -n Notes.Api.Tests -o tests/Notes.Api.Tests
dotnet sln add src/Notes.Api tests/Notes.Api.Tests
dotnet add tests/Notes.Api.Tests reference src/Notes.Api
dotnet add tests/Notes.Api.Tests package Microsoft.AspNetCore.Mvc.Testing
dotnet format
```

**Check:**
```powershell
dotnet build; dotnet test; dotnet format --verify-no-changes
```
All three succeed. (`dotnet test` runs the template's one empty test.)

### C3. Push it

```powershell
git add .
git commit -m "Sandbox: template + .NET skeleton"
gh repo create notes-sandbox --public --source . --push
```

It's **public** on purpose: free GitHub accounts can only protect branches on public repos. The sandbox has nothing secret. For private real projects you'd need GitHub Pro.

**Check:** on GitHub, the **Actions** tab shows a green `ci` run.

### C4. Protect `main` (gate 2: only you can merge)

On GitHub: **Settings → Rules → Rulesets → New ruleset → New branch ruleset**

- Name: `protect-main`, Enforcement: **Active**
- Target branches: **Add target → Include default branch**
- Turn on:
  - **Restrict deletions**
  - **Require a pull request before merging** (required approvals: **0**, because you can't approve your own PRs. You still review and click merge yourself)
  - **Require status checks to pass** → add `build-and-test`
  - **Block force pushes**
- **Create**

**Check:** try a direct push. It must be refused:
```powershell
git commit --allow-empty -m "test direct push"
git push
```
GitHub rejects it. Then undo the local commit: `git reset --hard HEAD~1`.

> Why this matters: the deny rules in `.claude/settings.json` and `opencode.json` are **guardrails, not walls**. Claude Code's own docs say an agent could get around a rule like `git push * main` by writing the command differently. GitHub's protection can't be got around from your computer, so that is the real gate.

---

## Part D: First test run

Open Claude Code in the sandbox:

```powershell
cd C:\dev\notes-sandbox
claude
```

### D1. One-time project setup

```
/setup-matt-pocock-skills
```
Answer: GitHub Issues as the tracker, `docs/` as the docs folder.

### D2. Scope the first slice (you + main session)

The spec is already drafted with open questions:
```
/grill-with-docs docs/specs/001-notes-crud.md
```
Answer its questions. If it answers its own questions, stop it and say: "List every question you answered yourself, and ask me each one."

### D3. Run the pipeline

Paste this:

```
Run the pipeline for spec 001 as described in CLAUDE.md:
1. /to-spec to mark the spec Approved.
2. /to-tickets to create the GitHub issues.
3. Use the architect to check the spec and tickets and label each ticket simple or normal. Show me its report.
4. Start with ticket 1 only, in its own worktree, using the builder its label calls for.
5. When CI is green, use the reviewer. Follow the review rules in CLAUDE.md.
6. Stop when ticket 1 passes review and tell me the PR is ready. Wait for me.
```

If the main session can't start `/to-spec` or `/to-tickets` itself, type them yourself, then paste from step 3.

### D4. Your part on each ticket

1. Open the PR on GitHub. Read the reviewer's verdict and skim the diff.
2. Check that CI is green. You can't merge otherwise.
3. Click **Merge**.
4. Add a row to `docs/pilot-log.md`.
5. Tell Claude Code: "Ticket 1 merged. Continue with the next ticket(s); run independent ones in parallel."

### D5. The benchmark (after the first 2–3 tickets work)

Follow `docs/agents/benchmark.md`: same tasks, Kimi vs Sonnet, on two or three days. The numbers decide your lineup.

---

## Part E: When something goes wrong

| Problem | Fix |
| --- | --- |
| Kimi: "No endpoints found matching your data policy" | OpenRouter **Settings → Privacy**: allow free endpoints (sandbox only), or use the Sonnet builder |
| Kimi: 429 / rate limit | Rerun that ticket with the Sonnet builder and note it in the log. If it keeps happening, set up FreeLLMAPI (A6) |
| `opencode run` hangs | Ctrl+C. Make sure the command was started with the `GIT_EDITOR=false GIT_PAGER=cat GIT_TERMINAL_PROMPT=0` prefix from CLAUDE.md |
| Codex: model not found | Use a model your account has; update `reviewer.md` |
| CI fails on `dotnet format` | Run `dotnet format` locally, commit, push |
| An agent says it's "done" but CI is red | Expected: the gate caught it. Send the CI log back to the builder |
| Same review problem twice | The main session moves it up a model tier; after round 3 it stops and asks you |
| context-mode hook timeout | Ignore it if it's rare; if it repeats, see the earlier fix (install Bun, Defender exclusion) |
