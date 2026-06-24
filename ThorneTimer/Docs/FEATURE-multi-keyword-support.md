# Multi-Keyword Support Investigation

**Date**: 2024  
**Query**: Can we store multiple keywords in StartKeyword/EndKeyword separated by | so a timer can trigger on multiple scenarios?

---

## Current Implementation

### Keyword Storage
- **File**: ThorneTimer/Database.cs, lines 104 and 111
- **Schema**:
  - 	imers.StartKeyword — TEXT column for start trigger keyword
  - 	imers.EndKeyword — TEXT column for end/stop trigger keyword
  - categories.StartKeyword — TEXT column for category activation keyword
  - categories.EndKeyword — TEXT column for category deactivation keyword

### Keyword Matching Logic
- **File**: ThorneTimer/TimerRuntime.cs, ProcessLogText() method (lines 278–344)

**Current implementation**: Simple .Contains() or .IndexOf() checks on a single keyword string. No pipe-separator support.

---

## Proposed: Pipe-Separated Multi-Keyword Support

### Concept
Allow users to enter multiple keywords separated by | (pipe), so a timer triggers if **any one** of the keywords matches:

**Example**:
- **StartKeyword**: You begin casting|You start chanting|You raise your hands
- **Meaning**: Timer starts when log contains ANY of these phrases
- **Use case**: Trigger on multiple spell animations or different class ability descriptions

### Implementation

Add helper method to TimerRuntime.cs:

\\\csharp
private bool SplitAndMatchKeywords(string keywordString, string chunk, bool caseSensitive)
{
    if (string.IsNullOrEmpty(keywordString))
        return false;

    var keywords = keywordString.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    
    foreach (var keyword in keywords)
    {
        string trimmedKeyword = keyword.Trim();
        if (trimmedKeyword.Length == 0) continue;

        if (caseSensitive)
        {
            if (chunk.IndexOf(trimmedKeyword, StringComparison.Ordinal) >= 0)
                return true;
        }
        else
        {
            if (chunk.IndexOf(trimmedKeyword, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
    }
    return false;
}
\\\

Then update ProcessLogText() calls to use this method instead of direct .Contains() / .IndexOf().

---

## Benefits

? **Backward Compatible**: Single keywords work unchanged  
? **No Schema Changes**: Reuses existing TEXT columns  
? **Simple Implementation**: ~30 lines of code  
? **Flexible**: OR logic handles multi-scenario timers  

---

## Use Cases

- Trigger on multiple spell cast animations
- Detect zone entry via different log formats
- Buff applied (class-specific descriptions)
- Pet summoned (class-dependent names)

---

## Status

Currently **NOT IMPLEMENTED** in the codebase. This is a feature enhancement proposal.
