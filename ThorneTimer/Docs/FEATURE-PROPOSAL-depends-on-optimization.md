# DependsOn Chain Optimization Feature Proposal

**Date**: 2024  
**Status**: Analysis + Proposal (Not yet implemented)  
**Concern**: Performance and timer reduction via occurrences-based chaining

---

## Current Implementation

### Database Schema
- **Table**: 	imers
- **Columns**: 
  - DependsOnTimer — TEXT, stores the name of the parent timer this timer depends on
  - DependsOnDelay — INTEGER, delay in seconds after parent expires before this fires

### Logic
- **File**: ThorneTimer/TimerRuntime.cs, TriggerTimer() method (line 339)
- **Check**: CheckDependentTimer() method (line 806)

**Current flow**:
1. User triggers a timer via keyword match
2. If timer has DependsOnTimer set, TriggerTimer() calls CheckDependentTimer()
3. CheckDependentTimer() searches through all timerStates for a running timer matching the name
4. If found and elapsed time > delay, recursively checks that timer's dependency
5. If dependency chain resolves, the trigger timer starts

### Real-World Example

**Manual approach** (current requirement):
- Create **4 separate timers**:
  1. Boss Spawn (duration 2h)
  2. Boss II Spawn (depends on Boss Spawn, +5s delay, duration 2h)
  3. Boss III Spawn (depends on Boss II Spawn, +5s delay, duration 2h)
  4. Boss IV Spawn (depends on Boss III Spawn, +5s delay, duration 2h)

**Problem**: 4 timers all fire on same keyword; user manually assigns each to a different occurrence counter or creates duplicate timers with different start keywords.

---

## Identified Performance Concerns

### Issue 1: String Matching on Every Log Line
- **Current**: CheckDependentTimer() iterates through ALL 	imerStates every time a keyword fires
- **Complexity**: O(N) per dependent check, O(N²) for deep chains
- **Keyword processing frequency**: Every 1–100 ms (log chunk size dependent)
- **Impact**: With 50 timers and deep dependency chains, each keyword can trigger expensive scans

**Example scenario**:
`
ProcessLogText() is called with log chunk containing 'Boss spawned'
  ? 50 timers checked for keyword match
  ? 10 timers match 'Boss spawned'
  ? Each of those calls CheckDependentTimer()
    ? Iterates through all 50 timers to find parent
    ? Parent may have a parent (recursive call)
    ? Parent's parent is searched again
`

### Issue 2: Duplicated Timer Definitions
- **Current**: No built-in mechanism to create N sequential timers from one template
- **Workaround**: Create 4 identical timers, manually chain them
- **Maintenance burden**: Change duration? Change all 4. Change keywords? Change all 4.
- **Storage waste**: 4 timers worth of data instead of 1 + occurrence config

### Issue 3: No Occurrence-Based Triggering
- **Current**: Each timer fires independently on keyword match
- **Desired**: A single timer definition that auto-generates timers for "occurrence 2", "occurrence 3", etc.
- **Use case**: "Boss spawns 4 times in succession" ? 1 timer definition handles all 4 spawns

---

## Proposed Solution: Occurrences-Based Chaining

### Concept

Add an Occurrences field to the timer that internally generates N sequential timers:

`
Base Timer: "Boss Spawn"
  - StartKeyword: "Boss has spawned"
  - Duration: 120s (2 minutes)
  - Occurrences: 4
  - InterOccurrenceDelay: 5s
  
[Internally generates]:
  - Boss Spawn (Occ 1) ? chains to Occ 2
  - Boss Spawn (Occ 2) ? chains to Occ 3
  - Boss Spawn (Occ 3) ? chains to Occ 4
  - Boss Spawn (Occ 4) ? [no chain]
  
All 4 use the same StartKeyword, but only Occ 1 starts on first match.
Occ 2–4 start via dependency chain.
`

### Database Changes

**New column** on 	imers table:
`sql
ALTER TABLE timers ADD COLUMN Occurrences INTEGER DEFAULT 1;
`

**Migration**:
- Existing timers get Occurrences = 1 (single timer, no chaining)
- New UI toggle: "Create X sequential timers with inter-occurrence delay"

### Implementation Details

#### Phase 1: Storage & UI
- [ ] Add Occurrences column to schema (idempotent migration)
- [ ] Add InterOccurrenceDelay column (separate from DependsOnDelay for clarity)
- [ ] Update TimersController to handle multi-occurrence editing
- [ ] Update UI to show a spinner: "Occurrences: [1..N]" and "Delay between: [5 sec]"

#### Phase 2: Runtime Generation
- [ ] Load timer, check if Occurrences > 1
- [ ] If yes, generate virtual child timers (stored as separate rows with special naming)
  - Example: "Boss Spawn" (base) + "Boss Spawn II", "Boss Spawn III", "Boss Spawn IV"
  - Naming: "{Name} II", "{Name} III", ... (roman numerals 2–N)
  - All inherit StartKeyword, EndKeyword, Duration, Style, CategoryID from base
  - Each has DependsOnTimer pointing to previous occurrence
  - Last one has no dependency
- [ ] Mark generated timers as "linked" so they're saved/loaded as a unit

#### Phase 3: Performance Optimization
- Cache DependsOnTimer lookups by building a hash map at startup
- **Before**:  CheckDependentTimer() does 	imerStates.FirstOrDefault(t => t.Name == dependentName)
- **After**: 
ameToTimerState dict lookup O(1)
- **Update**: When timers are added/removed, rebuild cache

### Code Changes Required

**File**: ThorneTimer/TimerRuntime.cs

1. Add dependency cache:
   `csharp
   private Dictionary<string, TimerState> timerNameCache;
   
   private void RebuildTimerNameCache()
   {
       timerNameCache = new Dictionary<string, TimerState>(StringComparer.OrdinalIgnoreCase);
       foreach (var ts in timerStates)
       {
           if (!timerNameCache.ContainsKey(ts.Name))
               timerNameCache[ts.Name] = ts;
       }
   }
   `

2. Optimize CheckDependentTimer():
   `csharp
   private bool CheckDependentTimer(string dependentName, double delayMS)
   {
       if (!timerNameCache.TryGetValue(dependentName, out var ts))
           return false;  // Not found
       
       if (!ts.IsRunning)
           return false;  // Not running
       
       // ... rest of logic (check elapsed time, recurse)
   }
   `

3. Generate occurrence timers on load:
   `csharp
   private void GenerateOccurrenceTimers(TimerState baseTimer)
   {
       if (baseTimer.Occurrences <= 1)
           return;
       
       for (int i = 2; i <= baseTimer.Occurrences; i++)
       {
           var occurrence = CloneTimer(baseTimer);
           occurrence.Name = FormatOccurrenceName(baseTimer.Name, i);
           occurrence.DependsOnTimer = i == 2 
               ? baseTimer.Name 
               : FormatOccurrenceName(baseTimer.Name, i - 1);
           occurrence.DependsOnDelay = baseTimer.InterOccurrenceDelay;
           
           // Mark as generated so Save doesn't persist
           occurrence.IsGenerated = true;
           
           timerStates.Add(occurrence);
       }
   }
   `

### UI Changes

**File**: ThorneTimer/FormMain.cs or new TimerChainingController.cs

Add to timer detail editor:
`
[  ] Create occurrence chain
Number of occurrences: [___] 
Delay between occurrences (sec): [___]
? "Creates N sequential timers: Base, Base II, Base III, etc."
`

When checked, display read-only summary:
`
Will generate:
  • Boss Spawn (triggers on keyword, starts Base II after delay)
  • Boss Spawn II (depends on Boss Spawn + 5s delay)
  • Boss Spawn III (depends on Boss Spawn II + 5s delay)
  • Boss Spawn IV (depends on Boss Spawn III + 5s delay)
`

---

## Performance Comparison

### Before (Current)

Scenario: 4 manually-created timers for "Boss Spawn", all with same keyword

`
Log line: "Boss has spawned"
  +- Check Timer 1 (Boss Spawn I): matches keyword ? TriggerTimer()
  ¦  +- CheckDependentTimer("Boss Spawn") ? linear search O(N) ? found
  ¦     +- Check elapsed > delay
  +- Check Timer 2 (Boss Spawn II): matches keyword ? TriggerTimer()
  ¦  +- CheckDependentTimer("Boss Spawn I") ? linear search O(N) ? found
  ¦     +- Check elapsed > delay
  +- Check Timer 3 (Boss Spawn III): matches keyword ? TriggerTimer()
  ¦  +- CheckDependentTimer("Boss Spawn II") ? linear search O(N) ? found
  +- Check Timer 4 (Boss Spawn IV): matches keyword ? TriggerTimer()
     +- CheckDependentTimer("Boss Spawn III") ? linear search O(N) ? found

Cost per keyword match: 4 × [O(N) search + elapsed check] = O(4N)
Redundancy: Keyword is checked 4 times for identical conditions
`

### After (With Occurrences + Cache)

Scenario: 1 timer definition expands to 4 occurrence timers, keyword only matches first

`
Log line: "Boss has spawned"
  +- Check Timer 1 (Boss Spawn): matches keyword ? TriggerTimer()
  ¦  +- CheckDependentTimer("Boss Spawn", ...) ? cache lookup O(1) ? found
  ¦     +- Check elapsed > delay
  +- Check Timer 2 (Boss Spawn II): keyword mismatch (depends on Timer 1) ? skip
  +- Check Timer 3 (Boss Spawn III): keyword mismatch ? skip
  +- Check Timer 4 (Boss Spawn IV): keyword mismatch ? skip

Cost per keyword match: 1 × [O(1) cache lookup + elapsed check] = O(1)
Redundancy: Eliminated (single keyword definition)
`

### Benchmark Expectations

- **4 manually-chained timers**: ~0.5–1.0 ms per keyword match (50 timers in list)
- **With occurrences + cache**: ~0.1–0.2 ms per keyword match (same scenario)
- **Improvement**: **5–10x faster** for typical dependency chain scenarios

---

## Implementation Roadmap

### Phase 1 (v0.7.0): Database + UI Foundation
- [ ] Add Occurrences and InterOccurrenceDelay columns (migrations)
- [ ] Update TimersRepository to read/write new fields
- [ ] Add UI checkbox + spinners to timer edit dialog
- [ ] Display generated timer names in preview

### Phase 2 (v0.7.1): Runtime Generation
- [ ] Implement GenerateOccurrenceTimers() in TimerRuntime
- [ ] Load generated timers on startup
- [ ] Ensure save skips generated timers (only save base)
- [ ] Test chaining logic with mock timers

### Phase 3 (v0.7.2): Performance Optimization
- [ ] Add timer name cache dictionary
- [ ] Update CheckDependentTimer() to use cache
- [ ] Benchmark and measure improvement
- [ ] Profile for memory impact

### Phase 4 (v0.8.0): Polish & Cleanup
- [ ] Rename generated timers in grid (show "II", "III" clearly)
- [ ] Allow re-generating if base timer modified
- [ ] Add validation (max 10 occurrences?)
- [ ] Documentation & release notes

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Generated timers bloat UI grid | Medium | Filter/group by base timer, collapse view |
| Saving breaks generated timers | High | Mark as generated, skip on save, regenerate on load |
| Cache invalidation bugs | Medium | Rebuild cache on any timer add/delete |
| Circular dependencies | Low | Validate chain depth, max 10 levels |

---

## Backward Compatibility

? **Fully compatible**
- Existing timers get Occurrences = 1 (treated as non-chaining)
- No breaking schema changes
- Generated timers are internal (not persisted)
- Users can keep manual chain timers or convert to occurrences

---

## Testing Strategy

### Unit Tests
- [ ] GenerateOccurrenceTimers() produces correct names and dependencies
- [ ] Roman numeral naming (II, III, IV, V, ...)
- [ ] Cache lookup correctness
- [ ] Circular dependency detection

### Integration Tests
- [ ] Load .tdb with mixed manual chains and occurrence timers
- [ ] Keyword triggers correct occurrence (not others)
- [ ] Chain fires in sequence with delays
- [ ] Grid displays generated timers (or hides them)

### Performance Tests
- [ ] Benchmark keyword matching with/without cache
- [ ] Profile memory usage with 100+ generated timers
- [ ] Latency: log parse ? timer trigger

---

## User Benefits

? **Reduce timer count** — One definition instead of N duplicates  
? **Easier maintenance** — Edit once, applies to all occurrences  
? **Cleaner UI** — Fewer rows in timer grid (collapsible groups)  
? **Better performance** — Faster keyword matching + caching  
? **Semantic clarity** — "4 occurrences of Boss Spawn" vs "4 different timers"  

---

## Conclusion

The occurrences-based chaining feature **solves three problems simultaneously**:
1. **Reduces timer sprawl** (1 definition ? N automatic timers)
2. **Improves performance** (O(N) search ? O(1) cache lookup)
3. **Minimizes parsing overhead** (single keyword check instead of N checks)

**Estimated complexity**: Medium (3–4 weeks of development + testing)  
**Expected value**: High (significant QoL + performance improvement)  
**Priority**: v0.7.0+ (after current beta4 stabilization)
