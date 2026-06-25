# Multi-Keyword Support

> **Status**: ✅ Implemented
> 
> **Type**: Feature Note
> 
> **Branch**: `v0.6.0-gui-enhancements`
> 
> **Implemented In**: `db06029`

---

## Summary

Start and end keyword fields now support **pipe-separated OR matching** for both:

- Timer keywords (`timers.StartKeyword`, `timers.EndKeyword`)
- Category keywords (`categories.StartKeyword`, `categories.EndKeyword`)

Example:

- `StartKeyword`: `You begin casting|You start chanting|You raise your hands`
- Behavior: timer starts if **any** listed keyword matches the log chunk.

---

## Storage

No schema changes were required.

- `timers.StartKeyword` (TEXT)
- `timers.EndKeyword` (TEXT)
- `categories.StartKeyword` (TEXT)
- `categories.EndKeyword` (TEXT)

---

## Runtime Implementation

**File**: `ThorneTimer/TimerRuntime.cs`

Implemented helper:

```csharp
private bool KeywordMatches(string keywordString, string chunk, bool caseSensitive)
```

Behavior:

1. Split input on `|`
2. Trim each token
3. Skip empty entries
4. Match each token against the log chunk
5. Return true on first match (OR semantics)

`ProcessLogText()` now uses `KeywordMatches(...)` for timer and category start/end checks.

---

## Compatibility

- ✅ Existing single-keyword entries continue to work unchanged
- ✅ Case-sensitive mode still respected per timer (`CaseYn`)
- ✅ No DB migration required

---

## Validation Checklist

- [x] Timer start keyword accepts `keyword1|keyword2`
- [x] Timer end keyword accepts `keyword1|keyword2`
- [x] Category start keyword accepts `keyword1|keyword2`
- [x] Category end keyword accepts `keyword1|keyword2`
- [x] Single keyword behavior unchanged
- [x] Release build passes

---

## Notes

This file is an implementation record (not a proposal).
For pending design work, use proposal/planning docs and `Docs/STATUS.md`.
