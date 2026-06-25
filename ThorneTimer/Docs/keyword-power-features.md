# Keyword Power Features — Design

> **Status:** 📐 Design / Spec — pre-implementation
> **Version:** v0.7.0 ("Smarter Timer Authoring")
> **Branch:** `v0.7.0-dev`
> **Date:** 2026-06-25
> **Author:** Draknaré Thorne / GitHub Copilot
>
> This document is the **detailed, durable specification** for the keyword-matching
> upgrade. If chat context is lost, this file is the source of truth for the agreed
> behavior (matching tiers, compiled-matcher architecture, capture-group templates,
> schema, and the performance/benchmark plan). Adjust this doc **before**
> implementation begins, and update it as each phase in §10 lands.

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
   This drives a hard rule for the whole feature: **all keyword syntax is
   classified and compiled at load/edit time** — the hot path never inspects a
   keyword to decide *if* or *how* to handle `*`, `?`, anchors, or pipes. By the
   time a chunk arrives, every term is already a prepared literal or a compiled
   `Regex`; matching just executes it.

---

## 3. Matching tiers (escalating cost, opt-in)

A timer keyword is classified **once at load time** into the cheapest tier that
satisfies it. Most timers stay on the fast path.

| Tier | Trigger syntax | Engine | Relative cost | Captures? |
|------|----------------|--------|---------------|-----------|
| **0 — Literal** | plain text (today) | `IndexOf(Ordinal[IgnoreCase])` | 1× (baseline) | No |
| **1 — Wildcard** | contains `*` and/or `?` (glob) | compiled `Regex`, cached | ~2–5× | Optional |
| **2 — Regex** | wrapped `^…$` or `/…/` escape hatch | compiled `Regex`, cached | ~3–8× | Yes |

Design rules:

- **Literal stays literal.** A keyword with no `*` and no regex escape is never
  promoted to `Regex` — it keeps using `IndexOf`. This guarantees the common case
  has **zero** added cost versus today.
- **Glob is sugar over regex.** `*` compiles to `.*?` and `?` to `.` (single
  char); the rest of the token is `Regex.Escape`-d, so `Cloud of * fades` becomes
  `Cloud\ of\ .*?\ fades` and `Gate?` becomes `Gate.`. Compiled with
  `RegexOptions.Compiled | CultureInvariant` (+ `IgnoreCase` unless `CaseYn`). The
  glob→regex translation happens **once at load/edit time**, never per chunk.
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
└─ bool HasCaptureGroups            // tier 1 (implicit * group) or tier 2 (named/
                                    // numbered groups); drives template population
```

- Built in/around `TimerRuntime` when `timerStates` / `categoryStates` load, and
  rebuilt for a single timer on edit (cheap, off the hot path). **Classification
  (literal vs. glob vs. regex) and glob→regex translation happen here, exactly
  once** — so the match loop never parses syntax or branches on `*`/`?`/anchors.
- `ProcessLogText` calls `matcher.Matches(line, out captures)` instead of
  `KeywordMatches(...)`. For `IsLiteralOnly` matchers this is a straight `IndexOf`
  loop — **no allocation, no regex** — preserving the baseline. Each `KeywordTerm`
  already knows its tier, so dispatch is a single branch on a precomputed enum,
  not a string inspection.
- The `Regex` objects are reused for the life of the matcher; capture extraction
  only runs on a term that actually matched **and** has `HasCaptureGroups` set
  (any tier-2 group, or a tier-1 glob whose `*` became an implicit capture — see
  §5.5). Tier-0 literals never extract captures.

---

## 5. Capture groups → speech / display templates

This section defines the **lifecycle**: how a keyword *detects* an event, how
values are *pulled out* of the matched line, and how those values then *drive*
speech and/or the mini-view label. The keyword is the trigger; the templates are
the output. They are independent — a timer can use either, both, or neither.

### 5.1 The scenario this enables

A vendor's reply in the log:

```
Vendor says, 'I'll give you 1 platinum and 2 gold for a Bronze Dagger.'
```

The user wants the mini view to show `Bronze Dagger — 1p 2g` and (optionally) hear
"one platinum two gold". To do that we must (a) **recognize** this is a vendor
sale line, (b) **extract** the price and item, and (c) **render** them into the
display label and/or the spoken phrase. Steps (a)/(b) are one regex with capture
groups; step (c) is a template applied to those captures.

### 5.2 Lifecycle (detect → capture → render)

```
                       ┌─────────────────────────────────────────────┐
log line ─▶ matcher ──▶│ matched? ──no──▶ (nothing; next line)        │
(whole line, §6)       │     │yes                                     │
                       │     ▼                                        │
                       │  captures = { {1}="1", {2}="2",              │
                       │               {item}="Bronze Dagger", … }    │
                       └─────┬───────────────────────────────────────┘
                             │  (resolution runs OUTSIDE the match loop)
              ┌──────────────┼───────────────────────────┐
              ▼              ▼                             ▼
      DisplayNameTemplate  SpeechTemplate            (timer/feed action
      "{item} — {1}p {2}g" "{1} platinum {2} gold"    as today: start,
              │              │                          reset, sound)
              ▼              ▼
        mini-view label   spoken via VoiceManager
        (TimerStateChanged) (TimerSoundRequested)
```

1. **Detect.** A keyword term matches the whole line (§6). For this scenario the
   term is a tier-2 regex with capture groups, e.g.
   `I'll give you (\d+) platinum and (\d+) gold for a (?<item>.+?)\.`
   (a `*` glob can stand in for the looser parts — see §5.5).
2. **Capture.** Because the matched term `HasCaptureGroups`, the engine pulls the
   group values into a small capture map (`{1}`, `{2}`, `{item}`, …). This runs
   **only on a confirmed match**, never on the non-matching majority of lines.
3. **Render.** The capture map is substituted into whichever templates are set:
   - `DisplayNameTemplate` → the label shown in the grid / mini view.
   - `SpeechTemplate` → the phrase handed to `VoiceManager` for TTS.
   Each is independent and optional.

### 5.3 New columns (idempotent migration, default NULL = today's behavior)

- `timers.SpeechTemplate` (TEXT) — overrides what is **spoken**. NULL ⇒ today's
  behavior (speak the literal `SpeechText`, or nothing if unset).
- `timers.DisplayNameTemplate` (TEXT) — overrides the **label** shown in the grid
  and mini view. NULL ⇒ today's behavior (show the timer `Name`).

Both are pure additions; an existing `.tdb` with no templates behaves exactly as
it does now.

### 5.4 How the templates relate to the existing `SpeechText` / label columns

The templates do **not** introduce a new output path — they feed the *same*
events the runtime already raises. Today a started/updated timer raises
`TimerStateChanged` (→ `FormMain` updates the grid/mini view) and
`TimerSoundRequested` (→ `FormMain` plays a sound or speaks). The change is
purely *what string* those handlers receive:

| Output | Today (no template) | With template (capture groups present) |
|---|---|---|
| Mini-view / grid label | timer `Name` | `DisplayNameTemplate` resolved against captures |
| Spoken phrase | literal `SpeechText` | `SpeechTemplate` resolved against captures |

So conceptually the "speech column" you remembered **does** keep working — the
`SpeechTemplate` simply *replaces* the literal speech string at render time when
the matched keyword produced captures. If a timer has no template, or the matched
term has no capture groups, the literal value is used and nothing changes.

### 5.5 Template mechanics

- Numbered placeholders `{0..n}` map to regex capture groups by index; named
  placeholders `{name}` map to `(?<name>…)`. Named groups are preferred for
  readability (`{item}`, `{price}`), positional are fine for quick rules.
- A glob `*` becomes an *implicit* capturing group so simple wildcard rules can
  still feed a template (e.g. `… for a *` exposes `{1}` = the item text) without
  the user writing regex. This translation is done **once at load/edit time**,
  consistent with §3/§4 — the hot path never inspects template or glob syntax.
- An unresolved placeholder (no such group, or empty capture) renders as empty
  string and is logged once at Debug — it never throws on the parse path.
- **Resolution happens after a match is confirmed and outside the tight match
  loop**, so non-matching chunks pay nothing. Only matched, capture-bearing terms
  do substitution.

### 5.6 Boundary with Feed Views (where the bigger vision lives)

The display/speech templating here is the **engine** for the larger
**Feed Views & Log Synthesis** vision in [`Docs/ROADMAP.md`](../../Docs/ROADMAP.md)
(vendor prices scrolling next to the merchant window, spoken as "one platinum two
gold"). This document scopes the **matching + capture + template substitution**
primitives onto the existing timer/mini-view path. The dedicated **feed**
*renderer*, the `ViewType = Feed` column, multi-target fan-out, and the speech
*sanitizer* (`1p 2g` → "one platinum two gold") are tracked there and build on
top of these primitives — they are intentionally **out of scope** here so this
feature stays shippable on its own.

---

## 6. Line-aware matching (prerequisite for regex/anchors)

Because chunks split mid-line, the regex tier needs whole lines.

- Add a small **line reassembly buffer** in `LogMonitor` (or a thin shim before
  `ProcessLogText`): accumulate bytes, split on `\n`, and dispatch **complete
  lines**; hold a trailing partial line until its newline arrives.
- **Decision (locked): line assembly is always-on for all tiers.** Every chunk is
  reassembled into complete lines before `ProcessLogText` sees it, so literal,
  wildcard, and regex terms all match against whole lines and `^`/`$` are always
  meaningful. This trades a small, bounded amount of buffering (one trailing
  partial line) for correctness and a single uniform code path. The buffering
  overhead is included in the §7 benchmark so we can confirm it stays negligible.

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

## 11. Decisions & open questions

**Resolved:**

- ✅ **Line assembly: always-on** for all tiers (see §6). Correctness over lazy
  buffering; overhead verified by the §7 benchmark.
- ✅ **Glob syntax: `*` and `?`** (`*` → `.*?`, `?` → `.`). Both are translated to a
  compiled `Regex` **once at load/edit time** — the hot path never inspects glob
  syntax. The `*`-vs-`?` cost difference is measured per the phasing in §10.
- ✅ **Phasing: incremental and individually measured** (see §10). Each tier ships
  and is benchmarked on its own so any regression is attributable.

**Still open (resolve during implementation):**

- Where the matcher cache lives — inside `TimerRuntime`, or a dedicated
  `KeywordMatcherFactory`? Prefer a small factory to keep `TimerRuntime` lean and
  testable.
- Throttling (`MinTriggerIntervalSeconds`) — ship in this feature or defer? Listed
  as a stretch item.
