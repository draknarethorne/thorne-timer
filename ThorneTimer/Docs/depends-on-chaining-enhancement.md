# Timer Dependency Chaining Enhancement

> **Status**: 📋 Design Proposal — **awaiting refinement decisions before implementation**
>
> **Question**: Should DependsOn support numeric occurrence-based chaining, name-based chaining,
> or both? How do we preserve explicit per-instance control in the UI while optimizing lookup
> performance?

**Version**: v0.6.x  
**Branch**: 0.6.0-gui-enhancements  
**Date**: 2026-06-05  
**Context**: Timer dependency chains today use linear search and recursive validation, which
creates performance concerns and UI ambiguity when many timers share a dependency. This proposal
explores whether numeric occurrence semantics or hybrid approaches could reduce both.

---

## 1. Problem Statement

### 1.1 Current Behavior

**File**: ThorneTimer/TimerRuntime.cs, CheckDependentTimer() method (lines ~240-265)

Current semantics:
- DependsOn contains a **timer name** (string).
- When a timer's start conditions are met, CheckDependentTimer(dependsOnName) is called.
- It **linearly searches** 	imerStates for an active timer matching that name.
- If found and elapsed time exceeds the delay, it checks **recursively** if that timer also has a dependency.
- This is **O(N × depth)** per check—acceptable for small chains but problematic as timers multiply.

**Concerns**:
1. **Performance**: Each time a timer's start conditions are evaluated, we do a linear search. With 50+ timers per character, this adds up.
2. **Ambiguity**: Multiple timers with the same name cause undefined behavior (first match wins).
3. **UI clarity**: Users may not realize that separate rows with the same name are conflated in dependency evaluation.
4. **Duplicate rows**: If you want to track the same spell with different categories or styles, you create duplicate rows—visible in the grid and mini views, harder to manage.

---

## 2. Proposed Solutions

### 2.1 Option A: Numeric Occurrence-Based Chaining

**Concept**: Instead of timer *name*, use a **timer index or occurrence number** (1-10 or similar).

**Pros**:
- Fast: O(1) direct index lookup instead of O(N) string search.
- Unambiguous: Each timer instance is uniquely identified.
- Reduces ambiguity around multiple timers with the same name.

**Cons**:
- **Breaks UI model**: Users expect to see and control **separate rows**.
- **Not user-friendly**: "depends on timer #3" is opaque; users prefer names.
- **Fragile refactoring**: If a user deletes row 2, row 3 becomes row 2—all dependencies break.
- **Backward compatibility**: Existing .tdb files have text names in DependsOn; migration is complex.

### 2.2 Option B: Cache-Optimized Name-Based Chaining (Recommended Short Term)

**Concept**: Keep DependsOn as a **timer name** (string), but **cache lookups** so the first
evaluation builds a dictionary of 
ame -> [active timer IDs]. Subsequent checks hit the cache
(O(1)) instead of re-scanning 	imerStates.

**Pros**:
- **Preserves UI model**: Separate rows remain visible and controllable.
- **Fast**: Caching amortizes the O(N) cost across many checks.
- **Backward compatible**: No schema or user-visible changes.
- **Low risk**: Cache is internal; if timers change state, cache invalidates naturally.
- **Clear semantics**: DependsOn: "Spell Cooldown" still means "wait for any active timer named 'Spell Cooldown'".

**Cons**:
- Doesn't solve **ambiguity of multiple timers with the same name**; cache still maps 
ame -> [IDs].
  - *Mitigation*: Document that "if multiple timers share a name, DependsOn triggers when **any one** is ready".

### 2.3 Option C: Hybrid Model (Long-Term Refactor)

**Concept**: Add a **second optional field** DependsOnOccurrence (integer, nullable). If set, use
it for fast numeric lookup. If null, fall back to name-based lookup with caching.

**Pros**:
- **Gradual adoption**: Name-based users migrate at their own pace.
- **Future-proof**: As users add occurrence-based dependencies, performance improves automatically.

**Cons**:
- **Schema complexity**: Two fields instead of one; validation logic branches.
- **UI burden**: Need UI to support both modes; potential confusion.
- **Migration overhead**: User-facing docs must explain both pathways.

---

## 3. Instance Handling in the UI

### 3.1 Current Model: Separate Rows = Separate Control

Today, if you want two "Spell Cooldown" timers (one in category A, one in category B), you
create **two rows** in the Timers grid. Each row:

- Has its own Active checkbox.
- Has its own Start/Stop button.
- Has its own Count field visible in mini views.
- Can be paused/resumed independently.
- Can be deleted independently.

**Benefit**: Full granular control. You can stop one instance without affecting the other.

**Drawback**: More clutter if many duplicate-named timers exist. The grid can become hard to scan.

### 3.2 Proposed Behavior: No Change to Row Model

**All proposals above preserve the separate-rows model.** This is intentional:

- Option B (cache-optimized name-based) works seamlessly with separate rows.
- Option A (numeric occurrence) could theoretically collapse rows into a single row with "counts", but this **breaks current UX** and is not recommended.
- Option C (hybrid) could eventually support collapsed rows, but only if a **separate UI mode** is built.

**User-Facing Guarantee**: "If you create two 'Spell Cooldown' timers, you get two rows, and
each row can be controlled independently. If you set both to DependsOn: "Pull Started", then
**both** will wait for an active 'Pull Started' timer."

---

## 4. Performance Analysis

### 4.1 Baseline: Current Linear Search

**Scenario**: 50 timers total, 20 active at any given time.

**Cost per check**: O(N) = 50 iterations (worst case).

**Conclusion**: For the current scale, linear search is **not a performance bottleneck**. But as
the codebase scales or logging frequency increases, it becomes relevant.

### 4.2 Option B: Cache-Optimized Name-Based (O(1) After Cache Build)

**Benefit**:
- First check: O(N) to build cache (one-time per state change set).
- Subsequent checks: O(1) dictionary lookup.
- Over a batch of 50 checks, amortizes to ~O(N + 49) = O(1) amortized.

**Verdict**: **Recommended immediate action.** Low risk, high gain, backward-compatible.

---

## 5. Recommended Approach: Phase-Based Rollout

### Phase 1: Cache Optimization (v0.6.x, short term)

**Scope**: Implement Option B (cache-optimized name-based chaining).

**Testing**:
- Create a test .tdb with a 10-timer chain.
- Measure elapsed time before/after (should be imperceptible, but caching should reduce allocation).
- Verify dependency triggering still works correctly.

**Deliverables**:
- Updated TimerRuntime.cs with caching logic.
- Updated this design document with Phase 1 completion note.
- Commit: eat(timers): add dependency-check caching for performance.

**Estimated effort**: 2-4 hours (code + testing + docs).

---

### Phase 2: Hybrid Model (v0.7.x or later, long term)

**Decision point**: Revisit after Phase 1 lands and user feedback arrives.

**Not recommended for immediate action.**

---

## 6. Instance Handling Summary

| Aspect | Current | Proposed | Notes |
|--------|---------|----------|-------|
| **Multiple timers, same name** | Allowed | Allowed | Separate rows in grid. |
| **Dependency reference** | Timer name (string) | Timer name (string) | No change; cached for speed. |
| **Ambiguity resolution** | First match wins | Cache maps name -> [all active IDs]; **all** are evaluated | More transparent. |
| **Row control** | Independent start/stop/delete per row | Independent start/stop/delete per row | No change. |
| **Mini view display** | Separate rows per timer | Separate rows per timer | No change. |

---

## 7. Migration & Backward Compatibility

### 7.1 No Schema Changes (Phase 1)

Since Option B (recommended) is purely a runtime optimization, **no database migration is needed**.

- Existing .tdb files work unchanged.
- DependsOn remains a TEXT field with timer names.
- Cache is in-memory and rebuilt automatically.

---

## 8. Testing Strategy

### 8.1 Integration Tests

Load an existing .tdb file with dependency chains and verify:
- Timers with DependsOn set trigger at the correct time.
- Multiple timers with the same dependency trigger independently.
- Chain depth (nested dependencies) doesn't break evaluation.

### 8.2 Performance Baseline

Run a synthetic benchmark:
- Create 50 timers with various dependency patterns.
- Measure wall-clock time for 1000 dependency checks (before and after caching).
- Report the improvement ratio.

---

## 9. Implementation Checklist (Phase 1)

- [ ] Add _dependencyCache and _cacheValid fields to TimerRuntime.
- [ ] Implement InvalidateDependencyCache() method.
- [ ] Refactor CheckDependentTimer() to use cache.
- [ ] Call InvalidateDependencyCache() from TriggerTimer() and state-change sites.
- [ ] Verify Release build succeeds (0 errors, 0 warnings).
- [ ] Commit with message: eat(timers): add dependency-check caching for performance.

---

**Document Version**: 1.0  
**Last Updated**: 2026-06-05  
**Status**: 📋 Awaiting approval and Phase 1 implementation decision.
