# Benchmark: comparing models on the same work

Use this before you settle which model does which job. A single good or bad run is luck; a small fixed set of tasks run several times is evidence.

## The set

10 to 15 tasks taken from real tickets in this repo, each one:

- starts from a fixed commit (write it down),
- has a check you trust that the model never sees (a test file you add after the run, or a script),
- has a reference solution you or a strong model wrote.

Mix easy and awkward tasks: a plain endpoint, a validation rule, a bug fix, an edit in a file with near-duplicate functions, Arabic text that must survive, a refactor that must not change behaviour.

## Running it

For each task and each model:

```bash
git worktree add .worktrees/bench-<task>-<model> <fixed-commit>
cd .worktrees/bench-<task>-<model>
opencode run --agent builder --model <provider/model> --auto "<task text>"
# then copy in the hidden check and run it
```

Run each pair on two or three different days. Record in the table below.

| Task | Model | Run | Check passed? | Time | Tokens / cost | Notes |
| --- | --- | --- | --- | --- | --- | --- |

## Deciding

- A model is good enough for `simple` tickets if it passes most simple tasks on most runs.
- Cheaper wins ties.
- Re-run the set when a new model comes out instead of switching on hype.
