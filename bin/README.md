# bin/ - maintenance scripts

Small, dependency-free utilities for repetitive maintenance and integrity checks
(documentation hygiene, validation, etc.). This mirrors the `.bin` script folder
pattern used in the Thorne-UI repo: drop a focused `.py` (optionally a `.bat`
launcher) here, name it for what it does, and keep it stdlib-only when possible.

These scripts are NOT compiled into the app. They are tracked in the solution as a
`bin` Solution Items folder so they are visible and runnable from the repo root.
(The repo `.gitignore` re-includes this root `/bin/` folder despite the `[Bb]in/`
build-output rule; see the negation entries there.)

## Scripts

| Script | Purpose |
|---|---|
| `fix_markdown.py` | Scan/auto-fix Markdown docs. By default converts the section sign `§` -> `Section ` and repairs mojibake (line endings preserved); `--aggressive` also flattens decorative punctuation (dashes, arrows, smart quotes, ellipsis, math). Emoji, diagrams, accents, and (by default) decorative punctuation are kept. |
| `fix_markdown.bat` | Convenience launcher for `fix_markdown.py` (passes args through). |

## fix_markdown.py

Why it exists: repo Markdown is stored as UTF-8 WITHOUT a BOM. In viewers that
misread multi-byte UTF-8 as Windows-1252, the section sign `§` shows up as garbled
text and corrupts section references/headers, so by default this tool converts it
to the word `Section ` and repairs classic Latin-1 <-> UTF-8 mojibake - while
preserving line endings exactly. Decorative punctuation (em/en dashes, smart
quotes, arrows, ellipsis, math glyphs) renders fine in UTF-8-aware viewers and is
kept by default; pass `--aggressive` to flatten it to ASCII. Emoji and ASCII-art
diagrams are always left alone (see Preserved below).

Usage (run from the repo root):

```
python bin/fix_markdown.py                      check repo Docs (default), report only
python bin/fix_markdown.py --fix                fix section sign + mojibake in place
python bin/fix_markdown.py --fix --aggressive   also flatten dashes/arrows/quotes
python bin/fix_markdown.py --fix --strip-bom path\to\file.md
python bin/fix_markdown.py --check ThorneTimer/Docs Docs
```

Or via the launcher:

```
bin\fix_markdown.bat
bin\fix_markdown.bat --fix
```

Modes:

- `--check` (default): reports problems, changes nothing, exits 1 if any found
  (handy for a pre-commit or CI gate).
- `--fix`: rewrites files in place as UTF-8 without a BOM (line endings preserved).
- `--aggressive`: also convert decorative punctuation (dashes, arrows, smart
  quotes, ellipsis, math) to ASCII. Off by default - those characters render fine.
- `--strip-bom`: in `--fix`, also removes a UTF-8 BOM if present.

Fixed by default (converted to ASCII): the section sign `U+00A7` -> the word
`Section ` (so `§5` becomes `Section 5` and `§ 5.4` becomes `Section 5.4`), plus
repair of classic Latin-1 <-> UTF-8 mojibake. With `--aggressive`, also em/en
dashes, smart quotes, ellipsis, prose arrows, and a few math glyphs. The maps live
at the top of `fix_markdown.py` and mirror the conversion table in
`.github/copilot-instructions.md` ("Documentation" section).

Preserved (never flagged or changed): all emoji / pictographs (the project keeps
emoji - they render fine), accented letters in names (e.g. `U+00E9`, Draknaré) and
any other Unicode letter, box-drawing / block-element / geometric diagram glyphs
(`U+2500`..`U+259F` and curated shapes like triangles and ballot boxes), and -
unless `--aggressive` is given - decorative punctuation (dashes, arrows, smart
quotes, ellipsis, math). The diagram characters are intentional ASCII-art stored
as clean UTF-8; they only look garbled when a viewer misreads the file's encoding,
so the tool leaves them alone instead of flattening them to `+`/`-`/`|`. Line
endings (CRLF/LF) are preserved exactly. Anything else non-ASCII that is not a
letter, emoji, or diagram glyph (and is not converted) is reported as an
informational "review" note rather than auto-changed.

Default scan roots: `ThorneTimer/Docs` and `Docs`. Pass explicit files/folders to
override.

## Adding a new script

1. Create `bin/<verb>_<thing>.py` (e.g. `validate_links.py`, `fix_tables.py`).
2. Keep it stdlib-only when you can; document usage in the module docstring.
3. Add a one-line entry to the Scripts table above.
4. Register it in the solution: add it under the `bin` Solution Items folder in
   `Thorne-Timer.sln` (the IDE locks the `.sln` while open, so edit from a
   terminal or add via Solution Explorer > Add > Existing Item on the bin folder).
