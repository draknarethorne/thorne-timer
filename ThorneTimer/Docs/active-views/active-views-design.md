# Active Views Design

## Goal
Transform the current static set of mini-views into a configurable, user-defined set of views. Each view can be linked to:
- (a) Categories of timers
- (b) Individual timers

This enables flexible timer organization and supports multiple user workflows.

---

## Current State
- Mini-views are statically defined in code (hardcoded, not user-configurable).
- UI for categories and characters is streamlined and managed inline within tables.
- All logic is concentrated in `FormMain.cs`, which is growing large and harder to maintain.

---

## Desired State
- Views are defined/configured by the user (not hardcoded).
- Each view can be associated with one or more timer categories or individual timers.
- UI for managing views should be intuitive and scalable for future features.

---

## Design Considerations

### 1. Where to Manage Views
- **Dialog-based management:**
  - Pros: Cleaner UI, easier to add advanced options, less clutter in main form.
  - Cons: More clicks for the user, context switch between main and dialog.
- **Inline (table/grid) management:**
  - Pros: Fast edits, consistent with current approach for categories/characters.
  - Cons: Can get cluttered, harder to scale for complex view definitions.

### 2. UI/UX Patterns
- For simple entities (categories, characters), inline works well.
- For more complex, hierarchical, or configurable entities (views), dialog-based or wizard-style management is often preferred.
- Consider Model-View-ViewModel (MVVM) or MVP patterns for better separation of concerns, especially as the UI grows.

### 3. Code Organization
- Avoid further bloating `FormMain.cs`.
- Encapsulate view management logic in a new class or user control (e.g., `ViewManager`, `ViewEditorDialog`).
- Use events or interfaces to communicate between main form and view management components.

---

## Proposed Approach

### Phase 1: Minimal Change (Get It Working)
- Implement view management following the current inline/table pattern for rapid prototyping.
- Keep logic modular to allow easy refactoring later.
- Add a placeholder for dialog-based management if needed.

### Phase 2: Refactor for Scalability
- Move view management to a dedicated dialog or user control.
- Refactor main form to delegate view-related logic to new components.
- Consider applying MVVM/MVP for future-proofing.
- Evaluate if other entities (categories, timers, etc.) should also move to dialog-based management for consistency.

---

## Design Pattern Recommendation
- For long-term maintainability, use a pattern that separates UI, logic, and data (MVVM, MVP, or MVC).
- For WinForms, MVP is often the most natural fit.
- Start with a modular approach so you can migrate to a full pattern as the app grows.

---

## Next Steps
1. Prototype view management inline (Phase 1), but structure code for easy extraction.
2. Evaluate user experience and code maintainability.
3. Plan and execute refactor to dialog/user control (Phase 2) if needed.
4. Document lessons learned and update design for other UI areas as appropriate.

---

## Open Questions
- Should all entity management (categories, timers, views) eventually move to dialogs for consistency?
- How much configuration do users need for views (simple list, layout, filters, etc.)?
- What is the best way to link views to categories/timers in the UI?

---

*This document is a living design and should be updated as the implementation and requirements evolve.*
