# Phase C Priority Shift — Timer Maintenance Dialog

## Decision

**Phase C (Timer Maintenance Dialog)** has been re-prioritized as **v0.7.0** (next release after v0.6.0), moving ahead of Phase B (Ping Refactor).

## Rationale

### Current State Issues

v0.6.0 introduced snapshot/restore logic to preserve actively logging character timers during manual character switches. While functional, this creates architectural complexity:

- Main form grid allows manual character browsing during gameplay
- Character dropdown in main form serves dual purpose (gameplay + maintenance)
- Snapshot/restore required to prevent timer interference
- Complexity in distinguishing "displayed character" vs "actively logging character"

### Long-Term Vision

The intended architecture separates gameplay from maintenance:

```
┌─────────────────────────────────────┐
│  Main Form (Gameplay)               │
│  ┌───────────────────────────────┐  │
│  │  Grid: Always Actively        │  │
│  │  Logging Character (Read-Only)│  │
│  └───────────────────────────────┘  │
│  - Auto-switch enabled            │
│  - Mini-views show active timers  │
│  - No manual character browsing   │
│  - Clean, focused gameplay UI     │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│  Timer Maintenance Dialog           │
│  ┌───────────────────────────────┐  │
│  │  Grid: Any Character          │  │
│  │  (Full Edit Mode)             │  │
│  └───────────────────────────────┘  │
│  - Load any character             │
│  - Add/edit/delete timers         │
│  - Timers display frozen state    │
│  - No impact on active gameplay   │
└─────────────────────────────────────┘
```

### Why v0.6.0 Work Enables This

The snapshot/restore and `isActive` infrastructure built in v0.6.0 provides the foundation for Phase C:

✅ **`isActive` flag** — Dialog can load timers with `isActive=false` to prevent unintended countdown  
✅ **`GetActiveCharacterID()`** — Main form can query LogMonitor to always show active character  
✅ **Snapshot/restore pattern** — Already handles "editing one thing while another runs"  
✅ **Conceptual separation** — `activeCharacterID` (UI) vs logging character already distinguished  

### Benefits of Early Phase C

1. **Simplifies main form** — Remove character dropdown, remove manual switching complexity
2. **Cleaner UX** — Gameplay and maintenance clearly separated
3. **Reduces bugs** — Fewer edge cases around character switching during gameplay
4. **Leverages recent work** — v0.6.0 infrastructure maps directly to Phase C needs
5. **User request** — "I do need to get to the dialog maintenance work sooner than later"

### Phase B Can Wait

Phase B (Ping Refactor) is an internal refactoring with no user-facing benefits:

- Eliminates technical debt around Ping timer handling
- Important for long-term maintainability
- But not blocking any features or causing user pain
- Can be tackled after Phase C delivers user value

## Timeline

| Phase | Version | Focus |
|-------|---------|-------|
| Phase D++ | v0.6.0 | Performance + auto-pause (testing) |
| **Phase C** | **v0.7.0** | **Maintenance dialog (next priority)** |
| Phase B | v0.8.0 | Ping refactor (deferred) |
| Phase E | v0.9.0 | Class profiles |
| Phase F | v1.0.0 | Advanced automation |

## Implementation Notes

Phase C will reuse v0.6.0 patterns:

- Maintenance dialog calls `LoadTimerRuntime()` with logic to pass `isActive=false`
- Main form removes character dropdown, locks grid to `logMonitor.GetActiveCharacterID()`
- Snapshot/restore infrastructure ensures active character timers unaffected during maintenance
- Dialog grid configured as editable, main form grid configured as read-only

## References

- [ROADMAP.md](../../Docs/ROADMAP.md) — Updated timeline
- [character-scope-timer-pausing.md](character-scope-timer-pausing.md) — v0.6.0 infrastructure that enables Phase C
