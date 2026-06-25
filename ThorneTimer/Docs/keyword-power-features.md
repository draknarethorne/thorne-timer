# Keyword Power Features — Design

> **Status**: 📐 Design (proposed)
>
> **Type**: Feature Design
>
> **Target**: v0.7.0 ("Smarter Timer Authoring")
>
> **Branch**: `v0.7.0-dev`
>
> **Author**: Draknaré Thorne / GitHub Copilot

---

## 1. Goal

Make timer authoring faster and more forgiving by upgrading keyword matching from
plain substring checks to a tiered matching engine: **literal → wildcard → regex**,
with **capture groups** feeding speech/display templates. The headline constraint:
**do not regress log-parsing throughput**. Matching runs on the hot path for every
log chunk against every active timer, so the design treats parsing cost as a
first-class requirement, not an afterthought.

This document defines the matching model, the compiled-matcher architecture, the
capture-group/template surface, conflict detection, and — central to the whole
effort — the **performance instrumentation and benchmarking plan** built on the
existing `ThorneLog.Time(...)` PERF facility.

---

## 2. Current parsing reality (the baseline we must protect)

Establish the starting point before changing anything.

- `LogMonitor` reads the EQ log in **1024-byte ASCII chunks** (`ReadNewContent` /
  `PollLoopSingle`) and raises `LogChunkReceived` with the raw chunk text.
- `TimerRuntime.ProcessLogText(chunk)` takes `syncLock`, then iterates **every
  category** and **every active timer**, calling `KeywordMatches(...)` for each
  start/end keyword.
- `KeywordMatches` currently, **per call, per chunk**:
  1. `keywordString.Split('|')` — allocates a `string[]` every time.
  2. `.Trim()` each token — allocates a `string` per token.
  3. `chunk.IndexOf(token, Ordinal | OrdinalIgnoreCase)`.

### Cost model

```
cost ≈ Σ chunks × (categories + activeTimers) × keywordsPerEntry × O(chunkLength)
```

plus **per-call allocations** (`Split` + `Trim`) that scale with the same factors.
On a busy raid log with 130+ active timers, that allocation churn — not the
`IndexOf` itself — is the most likely GC/latency culprit today.

### Two pre-existing hazards the design must account for

1. **Chunk boundaries split mid-line.** A 1024-byte buffer can cut a log line in
   half. Substring `Contains` mostly tolerates this (a phrase split across two
   chunks simply misses that tick), but **anchored regex (`^`/`$`) and capture
   groups are meaningless on a half-line**. Line-aware matching becomes a
   prerequisite for the regex tier (see §6).
2. **No precompilation.** Keyword strings are re-parsed on every chunk. Anything
   we add (globs → regex) must be compiled **once** and cached, never per-chunk.

---

## 3. Matching tiers (escalating cost, opt-in)

A timer keyword is classified **once at load time** into the cheapest tier that
satisfies it. Most timers stay on the fast path.

| Tier | Trigger syntax | Engine | Relative cost | Captures? |
|------|----------------|--------|---------------|-----------|
| **0 — Literal** | plain text (today) | `IndexOf(Ordinal[IgnoreCase])` | 1× (baseline) | No |
| **1 — Wildcard** | contains `*` (glob) | compiled `Regex`, cached | ~2–5× | Optional |
| **2 — Regex** | wrapped `^…$` or `/…/` escape hatch | compiled `Regex`, cached | ~3–8× | Yes |

Design rules:

- **Literal stays literal.** A keyword with no `*` and no regex escape is never
  promoted to `Regex` — it keeps using `IndexOf`. This guarantees the common case
  has **zero** added cost versus today.
- **Glob is sugar over regex.** `*` compiles to `.*?`, the rest of the token is
  `Regex.Escape`-d, so `Cloud of * fades` becomes `Cloud\ of\ .*?\ fades`. Compiled
  with `RegexOptions.Compiled | CultureInvariant` (+ `IgnoreCase` unless `CaseYn`).
- **Full regex is an explicit escape hatch**, not the default, so users do not pay
  regex cost (or footguns) unless they opt in.
- **Pipe (`|`) OR-splitting is preserved** and now splits into a list of
  **compiled matchers**, each carrying its own tier.

---

## 4. Compiled-matcher architecture

Replace the per-chunk `Split/Trim/IndexOf` with a **precompiled matcher object**
built once when timers load (or when a keyword is edited).

```
KeywordMatcher                      // immutable, built once per keyword field
├─ IReadOnlyList<KeywordTerm> Terms // one per pipe-delimited alternative
└─ bool IsLiteralOnly               // fast-path flag: all terms are tier 0

KeywordTerm
├─ MatchTier Tier                   // Literal | Wildcard | Regex
├─ string Literal                   // tier 0 only (already trimmed)
├─ Regex Compiled                   // tiers 1–2 only (RegexOptions.Compiled)
└─ bool HasCaptureGroups            // tier 2: drives template population
```

- Built in/around `TimerRuntime` when `timerStates` / `categoryStates` load, and
  rebuilt for a single timer on edit (cheap, off the hot path).
- `ProcessLogText` calls `matcher.Matches(chunk, out captures)` instead of
  `KeywordMatches(...)`. For `IsLiteralOnly` matchers this is a straight `IndexOf`
  loop — **no allocation, no regex** — preserving the baseline.
- The `Regex` objects are reused for the life of the matcher; capture extraction
  only runs on tier-2 terms that actually matched.

---

## 5. Capture groups → speech / display templates

New nullable columns (idempotent migration, default NULL = unchanged behavior):

- `timers.SpeechTemplate` (TEXT) — e.g. `{1} says {2}` or `{name} resists`
- `timers.DisplayNameTemplate` (TEXT) — overrides the grid/mini-view label

Mechanics:

- Numbered groups `{0..n}` map to regex capture groups; named groups `{name}` map
  to `(?<name>…)`.
- Templates only resolve when the matched term `HasCaptureGroups`; otherwise the
  literal `SpeechText` / timer name is used (today's behavior).
- Resolution happens **after** a match is confirmed and **outside** the tight match
  loop, so it never taxes non-matching chunks.

---

## 6. Line-aware matching (prerequisite for regex/anchors)

Because chunks split mid-line, the regex tier needs whole lines.

- Add a small **line reassembly buffer** in `LogMonitor` (or a thin shim before
  `ProcessLogText`): accumulate bytes, split on `\n`, and dispatch **complete
  lines**; hold a trailing partial line until its newline arrives.
- Literal/wildcard tiers can still run on raw chunks for backward-compatible
  behavior, but routing everything through line assembly is cleaner and makes
  `^`/`$` meaningful. **Decision to confirm during implementation:** line-assemble
  for all tiers (simpler, slightly more buffering) vs. only when any regex term is
  present (lazy). Lean toward always-on line assembly for correctness, measured
  against the benchmark in §7.

---

## 7. Performance: instrumentation, metrics & benchmarks (core requirement)

This is the part the feature lives or dies on. We already have `ThorneLog.Time(...)`
emitting `PERF [label]: 12.3 ms` (no-op when logging is disabled) and existing PERF
lines around startup/character-switch. We extend that to the match path.

### 7.1 What to measure

- **Per-chunk match time** — wrap the `ProcessLogText` body:
  `using (ThorneLog.Time("ProcessLogText"))`. Establish a **before** baseline on a
  known log + tome, then compare after each tier lands.
- **Aggregate match stats** (low overhead, opt-in): a lightweight counter struct
  per poll window — chunks processed, total/avg/max match time, matches found,
  regex evaluations. Flush a single `PERF` summary line every N seconds rather than
  logging per chunk (per-chunk logging would itself become the bottleneck).
- **Tier histogram** — count terms by tier at load (`literal=120 wildcard=8
  regex=3`) so we can see how much of a tome is on the fast path.
- **Regex hotspots** — for tier-2 terms, track evaluation count + cumulative time
  keyed by timer, so a pathological pattern (catastrophic backtracking) is visible
  in the log instead of mysterious lag.

### 7.2 Guardrails baked into the design

- **Fast-path short-circuit**: `IsLiteralOnly` matchers never touch regex.
- **Compile once**: zero per-chunk allocation for literal terms; regex compiled at
  load.
- **Regex timeout**: construct every `Regex` with a `matchTimeout` (e.g. 50–100 ms)
  so a bad user pattern fails safe instead of hanging the poll loop.
- **Optional ordering heuristic**: evaluate literal terms before regex terms within
  a pipe group so a cheap literal can satisfy OR-logic before any regex runs.
- **Length pre-filter (candidate)**: skip a term whose required literal substring
  cannot be present (e.g. anchored prefix check) — measure whether it actually pays
  off before committing.

### 7.3 Benchmark harness

- A repeatable **replay benchmark**: feed a captured log file through
  `ProcessLogText` against a representative tome (the 156-timer example tome is a
  good stress case) and emit the §7.1 stats. This gives an apples-to-apples
  before/after number per tier and prevents "it feels fine" regressions.
- Capture a baseline number **first** (literal-only, today's code) so every later
  change is measured against it.

### 7.4 Alternative search approaches (explicitly on the table)

If the benchmark shows the per-timer linear scan is the real ceiling (not regex
itself), consider — in rough order of effort:

1. **Aho-Corasick / multi-pattern literal automaton** for the tier-0 set: match all
   literal keywords in **one pass** over the chunk instead of one `IndexOf` per
   timer. Big win when literal-timer counts are high; only covers literals.
2. **Combined per-style/per-category alternation regex** for wildcard/regex terms:
   compile one big `(?<t12>…)|(?<t37>…)` and map the winning group back to a timer.
   Fewer regex invocations, at the cost of build complexity.
3. **Two-stage filter**: a cheap Aho-Corasick literal pre-scan gates which regex
   terms are even worth evaluating (most chunks match nothing).

The design does **not** commit to these up front. The benchmark decides: ship the
compiled-matcher tiers first, measure, and only escalate to an automaton if the
numbers justify it. (Keep the matcher interface narrow so the engine behind it can
be swapped without touching `ProcessLogText`.)

---

## 8. Conflict detection (issue #33)

Author-time, **off the hot path** — runs in the editor, not the parser.

- When saving a timer, evaluate its literal/wildcard terms against other timers'
  terms for overlap (e.g. one is a substring/subset of another, or two globs that
  both match a sample line).
- Surface a non-blocking warning: "This keyword also matches *Spawn: Phinigel*."
- Purely advisory; never prevents saving.

---

## 9. Schema changes (all idempotent, one-shot)

| Table | Column | Type | Default | Purpose |
|-------|--------|------|---------|---------|
| `timers` | `SpeechTemplate` | TEXT | NULL | capture-group speech output |
| `timers` | `DisplayNameTemplate` | TEXT | NULL | capture-group label override |
| `timers` | `MinTriggerIntervalSeconds` | INTEGER | NULL/0 | throttle ping spam (stretch) |

No change to existing `StartKeyword` / `EndKeyword` storage — tier is **derived at
load time** from the existing text, so old tomes light up wildcards/regex with no
migration of keyword data. Follows the established `isFieldExist` idempotent pattern
in `Database.cs`.

---

## 10. Phasing

1. **Baseline + harness** — add the replay benchmark and PERF stats around the
   *current* `KeywordMatches`. Record the number. (No behavior change.)
2. **Compiled-matcher refactor** — replace per-chunk `Split/Trim/IndexOf` with
   precompiled literal matchers. Re-measure: should be **≤ baseline** (less
   allocation).
3. **Wildcard tier** — `*` globs compiled to cached regex. Measure.
4. **Regex tier + line assembly + capture groups** — anchors, named groups,
   timeouts. Measure.
5. **Templates** — `SpeechTemplate` / `DisplayNameTemplate` wiring.
6. **Conflict detection** — editor-side advisory.
7. **(Conditional) automaton** — only if §7.3 numbers demand it.

Each phase is independently shippable and independently measured.

---

## 11. Open questions

- Line assembly always-on vs. lazy (see §6) — resolve with the benchmark.
- Glob syntax surface: just `*`, or also `?` (single char)? Start with `*` only.
- Where does the matcher cache live — inside `TimerRuntime`, or a dedicated
  `KeywordMatcherFactory`? Prefer a small factory to keep `TimerRuntime` lean and
  testable.
- Throttling (`MinTriggerIntervalSeconds`) — ship in this feature or defer? Listed
  as stretch.
