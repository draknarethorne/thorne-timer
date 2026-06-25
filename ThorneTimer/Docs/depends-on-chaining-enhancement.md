# Timer Dependency Chaining Enhancement

> **Status**: 📋 Planned / Proposal
> 
> **Type**: Feature Proposal
> 
> **Branch**: `v0.6.0-gui-enhancements`
> 
> **Last Updated**: 2026-06-25

---

## Question

Should `DependsOn` support numeric occurrence-based chaining, name-based chaining, or both?
How do we preserve explicit per-instance control in the UI while improving runtime lookup performance?

---

## Current Behavior

**File**: `ThorneTimer/TimerRuntime.cs` (`CheckDependentTimer`)

Today:

- `DependsOnTimer` stores a timer name (string)
- Runtime walks `timerStates` linearly to find matching running timers
- Chained dependencies recurse by following `DependsOnTimer`

This is simple and backward-compatible, but can become less efficient as timer counts and chain depth grow.

---

## Options Considered

### Option A — Numeric Occurrence-Based Dependency

Use numeric references (e.g., occurrence/index) instead of names.

- ✅ Fast direct lookup
- ❌ Fragile with row reorder/delete
- ❌ Poor UX clarity vs readable names
- ❌ Migration complexity for existing `.tdb` data

### Option B — Cache-Optimized Name-Based Dependency (**Recommended short-term**)

Keep name-based semantics, add runtime cache (`name -> running timers`) for fast repeated lookups.

- ✅ Preserves existing UX and data
- ✅ No schema migration required
- ✅ Low-risk implementation
- ⚠️ Same-name ambiguity remains (documented behavior)

### Option C — Hybrid Mode (Name + Optional Occurrence)

Introduce optional numeric linkage while retaining name fallback.

- ✅ Future flexibility
- ❌ Added schema + UI complexity
- ❌ Not required for immediate branch goals

---

## UI/Instance Model Constraint (Must Preserve)

Current user expectation is explicit control via separate rows:

- independent start/stop
- independent active state
- independent count
- separate mini-view visibility/counting

Any dependency enhancement should keep this row-level control model intact.

---

## Recommended Path

### Phase 1 (v0.6.x polish candidate)

Implement Option B cache optimization in `TimerRuntime`.

Scope:

- Build/refresh in-memory dependency cache from current running timer states
- Use cache in dependency evaluation
- Invalidate cache on timer state transitions (start/stop/load)

### Phase 2 (future, optional)

Revisit hybrid modeling only if real-world usage requires it.

---

## Testing Focus

- dependency chains still gate starts correctly
- same-name dependency behavior remains deterministic/documented
- no regressions to manual start/stop behavior
- no regressions to mini-view row representation

---

## Notes

This file is a proposal. See `Docs/STATUS.md` for planning vs implemented tracking.
