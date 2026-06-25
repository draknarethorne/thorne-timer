# Phase C Priority Shift — Timer Maintenance Dialog

## Decision

**Phase C (Timer Maintenance Dialog)** has been re-prioritized as **v0.7.0** (next release after v0.6.0), moving ahead of Phase B (Ping Refactor).

## Rationale

### Current State Issues

The v0.6.0 main form still mixes gameplay with configuration. The character dropdown serves a dual purpose (gameplay + browsing), and Timers / Characters editing happens inline in the same form that displays the active runtime. The previously attempted complex snapshot/restore architecture (background character timer preservation) was **reverted** — the v0.6.0 model is intentionally simple: one active character at a time, with browsing handled as a read-only display concern. See `session-handoff-v0.6.0-logmonitor-fix.md` for the full reversion context.

What remains is the architectural split between **playing** and **maintaining**:

- Main form grid still allows manual character browsing during gameplay
- Character dropdown in main form serves dual purpose (gameplay + maintenance)
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

The v0.6.0 architecture provides the foundation for Phase C:

✅ **`isActive` flag** — `TimerRuntime.RestoreCharacterState(states, isActive)` already supports loading timers in a frozen state; the maintenance dialog can pass `isActive=false`.
✅ **`LogMonitor.GetActiveCharacterID()` / `selectedCharacterID`** — UI selection is already separated from file-growth detection, so the main form can lock its grid to the actively logging character.
✅ **Hybrid Designer + Controller + Repository pattern** — the Styles, Views, and Categories tabs already prove out the model the maintenance dialog will follow (designer shell + controller behavior + typed repository CRUD).
✅ **Conceptual separation** — `activeCharacterID` (UI) vs logging character is already a working distinction.

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

- Maintenance dialog uses the same Designer + Controller + Repository pattern as Styles/Views/Categories (e.g. `TimersMaintenanceController` + extended `TimersRepository`).
- Main form removes the editable timer grid path and locks its read-only display to `logMonitor.GetActiveCharacterID()`.
- Dialog grid loads timers with `isActive=false` so opening it never starts countdowns or affects the actively logging character.
- The `(None)` character path from v0.6.0 already proves the main form handles a no-active-character state cleanly.

## References

- [ROADMAP.md](../../Docs/ROADMAP.md) — Updated timeline
- [character-scope-timer-pausing.md](character-scope-timer-pausing.md) — v0.6.0 infrastructure that enables Phase C
