#!/usr/bin/env node
// Summarise one OpenCode session export and append it to docs/ticket-stats.csv.
// Usage:
//   opencode export <session-id> > .worktrees/ticket-<n>-session.json
//   node scripts/ticket-stats.mjs .worktrees/ticket-<n>-session.json --ticket <n>
// Add --no-csv to only print the summary.
import { readFileSync, existsSync, appendFileSync, mkdirSync } from "node:fs";
import { dirname } from "node:path";
import { createHash } from "node:crypto";

const args = process.argv.slice(2);
const file = args.find((a) => !a.startsWith("--"));
const ticket = (() => { const i = args.indexOf("--ticket"); return i >= 0 ? args[i + 1] : ""; })();
const writeCsv = !args.includes("--no-csv");
const csvPath = "docs/ticket-stats.csv";
if (!file) { console.error("Usage: node scripts/ticket-stats.mjs <session.json> --ticket <n>"); process.exit(1); }

// opencode export may print a line or two before the JSON; start at the first "{".
// PowerShell's ">" writes UTF-16, so detect the encoding instead of assuming UTF-8.
const buf = readFileSync(file);
const utf16 = (buf[0] === 0xff && buf[1] === 0xfe) || (buf.length > 1 && buf[1] === 0x00);
const raw = buf.toString(utf16 ? "utf16le" : "utf8").replace(/^\uFEFF/, "");
const s = JSON.parse(raw.slice(raw.indexOf("{")));
const info = s.info ?? {};
const msgs = s.messages ?? [];

const t = { input: 0, output: 0, reasoning: 0, cacheRead: 0, cacheWrite: 0 };
let cost = 0, steps = 0, first = Infinity, last = 0;
const tools = {}, seen = {};
let repeats = 0;

for (const m of msgs) {
  const mi = m.info ?? {};
  if (mi.time?.created) first = Math.min(first, mi.time.created);
  const end = mi.time?.completed ?? mi.time?.created;
  if (end) last = Math.max(last, end);
  if (mi.role === "assistant") {
    steps++;
    const k = mi.tokens ?? {};
    t.input += k.input ?? 0; t.output += k.output ?? 0; t.reasoning += k.reasoning ?? 0;
    t.cacheRead += k.cache?.read ?? 0; t.cacheWrite += k.cache?.write ?? 0;
    cost += mi.cost ?? 0;
  }
  for (const p of m.parts ?? []) {
    if (p.type !== "tool") continue;
    const name = p.tool ?? "unknown";
    tools[name] = (tools[name] ?? 0) + 1;
    const input = p.state?.input;
    if (input !== undefined && !String(JSON.stringify(input)).includes("[redacted")) {
      const h = createHash("sha1").update(name + JSON.stringify(input)).digest("hex");
      seen[h] = (seen[h] ?? 0) + 1;
      if (seen[h] > 1) repeats++;
    }
  }
}

const total = t.input + t.output + t.reasoning + t.cacheRead + t.cacheWrite;
const minutes = isFinite(first) && last > first ? ((last - first) / 60000).toFixed(1) : "";
const toolCalls = Object.values(tools).reduce((a, b) => a + b, 0);
const topTools = Object.entries(tools).sort((a, b) => b[1] - a[1]).map(([k, v]) => `${k}:${v}`).join(" ");
const model = info.model ? `${info.model.providerID}/${info.model.id}` : "";
const cachePct = total ? Math.round((100 * t.cacheRead) / total) : 0;
const fmt = (n) => n.toLocaleString("en-US");

console.log(`Ticket ${ticket || "?"}  ·  session ${info.id ?? "?"}  ·  agent ${info.agent ?? "?"}  ·  ${model}`);
console.log(`Time      ${minutes || "?"} min, ${steps} model steps, ${toolCalls} tool calls${topTools ? ` (${topTools})` : ""}`);
console.log(`Tokens    total ${fmt(total)} = new input ${fmt(t.input)} + cache read ${fmt(t.cacheRead)} (${cachePct}%) + output ${fmt(t.output)} + reasoning ${fmt(t.reasoning)}`);
console.log(`Cost      $${cost.toFixed(4)}`);
if (repeats) console.log(`WARNING   ${repeats} repeated identical tool calls: possible loop. Check the log.`);
if (toolCalls === 0 && steps <= 2) console.log("NOTE      Very short session. Is this the right one? Use the largest session for the ticket.");

if (writeCsv) {
  const header = "ticket,session,agent,model,started_utc,minutes,steps,tool_calls,input,cache_read,cache_write,output,reasoning,total,cost_usd,repeated_calls,tools\n";
  if (!existsSync(csvPath)) { mkdirSync(dirname(csvPath), { recursive: true }); appendFileSync(csvPath, header); }
  const started = isFinite(first) ? new Date(first).toISOString() : "";
  const row = [ticket, info.id, info.agent, model, started, minutes, steps, toolCalls, t.input, t.cacheRead, t.cacheWrite, t.output, t.reasoning, total, cost.toFixed(4), repeats, `"${topTools}"`];
  appendFileSync(csvPath, row.join(",") + "\n");
  console.log(`Saved     row added to ${csvPath}`);
}
