#!/usr/bin/env python3
"""
fix_markdown.py - scan (and optionally auto-fix) Markdown docs for "funky
characters": mojibake left over from bad encoding round-trips, and decorative
Unicode that renders as garbage in viewers that fall back to the system codepage
(Windows-1252) because the repo stores Markdown as UTF-8 *without* a BOM.

This is the Thorne Timer counterpart to the validate/fix scripts kept in the
Thorne-UI `.bin` folder: small, dependency-free, repeatable maintenance tools.

WHAT IT DOES
  --check (default) : report problems, change nothing, exit 1 if any found.
  --fix             : rewrite files in place (UTF-8, no BOM) applying the map.
  --aggressive      : ALSO convert decorative punctuation (dashes, arrows, smart
                      quotes, ellipsis, math glyphs) to ASCII. Off by default.
  --strip-bom       : also remove a UTF-8 BOM if present (only in --fix).

WHAT IT TARGETS BY DEFAULT (fixed / converted to ASCII)
  1. Mojibake markers - byte sequences that mean a UTF-8 file was decoded as
     Windows-1252 at some point (e.g. an em dash showing as A-tilde / euro junk).
     These are reported; --fix attempts the known reversible repairs.
  2. The section sign (U+00A7 -> "Section ", so "§5" -> "Section 5"). This is the
     one piece of punctuation that reliably renders as garbage and corrupts
     section references/headers, so it is always fixed.

  Decorative punctuation (em/en dashes, smart quotes, ellipsis, prose arrows, math
  glyphs, middot, bullets) renders fine in UTF-8-aware viewers and is KEPT by
  default. Pass --aggressive to flatten it to ASCII (see DECORATIVE_REPLACEMENTS).

WHAT IT PRESERVES (never flagged or changed, unless noted)
  - All emoji / pictographs: the user keeps emoji; they render fine and add value.
    Astral-plane emoji, U+FE0F, the U+2600..U+27BF block, media/arrow emoji.
  - Decorative punctuation (dashes, arrows, smart quotes, ellipsis, math glyphs)
    by default - it renders fine; only --aggressive converts it to ASCII.
  - Accented letters used in names, e.g. U+00E9 (Draknare), and any Unicode letter.
  - Box-drawing / block elements (U+2500..U+259F) and curated geometric/diagram
    glyphs (triangles, ballot boxes, squared plus, diagram arrows): intentional
    ASCII-art diagrams, stored as clean UTF-8. NOT "funky" - only a viewer
    misreading the file's encoding makes them look garbled. Left fully alone.

  Anything else non-ASCII that is not a letter/emoji/diagram glyph, not the section
  sign, and not in DECORATIVE_REPLACEMENTS is surfaced as an informational "review"
  note (not auto-changed).

USAGE
  python bin/fix_markdown.py                       # check repo Docs (default roots)
  python bin/fix_markdown.py --fix                 # fix them in place
  python bin/fix_markdown.py --fix --strip-bom path\\to\\file.md
  python bin/fix_markdown.py --check ThorneTimer/Docs Docs

EXIT CODES
  0 = clean (or fixed with --fix)
  1 = problems found in --check mode
  2 = bad arguments / nothing to scan
"""

from __future__ import annotations

import argparse
import re
import sys
import unicodedata
from pathlib import Path

# --------------------------------------------------------------------------- #
# Configuration
# --------------------------------------------------------------------------- #

# Default folders to scan when no explicit paths are given (relative to repo root).
DEFAULT_ROOTS = ["ThorneTimer/Docs", "Docs"]

# The section sign is the one character that reliably renders as garbage and
# corrupts section references/headers (e.g. "Section 5.4" written with the glyph),
# so it is ALWAYS converted to the word "Section " (spacing-aware, see apply_fixes).
SECTION_SIGN = "\u00A7"

# Decorative punctuation -> ASCII. These render FINE in UTF-8-aware viewers, so they
# are KEPT by default and only converted when --aggressive is passed (for strictly
# ASCII-only output). Keep this list EXPLICIT and tight; do not blanket-strip all
# non-ASCII (that would kill intentional emoji, diagram glyphs, and accented names).
# Mirrors the table in copilot-instructions.md.
DECORATIVE_REPLACEMENTS = {
    "\u2014": "-",     # em dash      -> hyphen
    "\u2013": "-",     # en dash      -> hyphen
    "\u2212": "-",     # minus sign   -> hyphen
    "\u2026": "...",   # ellipsis     -> three dots
    "\u2192": "->",    # rightwards arrow
    "\u21D2": "=>",    # rightwards double arrow
    "\u2190": "<-",    # leftwards arrow
    "\u2194": "<->",   # left-right arrow
    "\u00D7": "x",     # multiplication sign
    "\u2248": "~=",    # almost equal
    "\u2264": "<=",    # less-than-or-equal
    "\u2265": ">=",    # greater-than-or-equal
    "\u03A3": "sum",   # Greek capital sigma
    "\u00B7": "/",     # middot separator
    "\u2022": "-",     # bullet
    "\u201C": '"',     # left double quote
    "\u201D": '"',     # right double quote
    "\u2018": "'",     # left single quote
    "\u2019": "'",     # right single quote (apostrophe)
    # NOTE: Box-drawing characters (U+2500..U+257F), block elements, geometric
    # diagram glyphs, and ALL emoji are intentionally NOT in this map. They render
    # fine and are legitimate content; see is_geometric / is_emoji (KEEP signals).
}

# Characters that are explicitly allowed and must never be flagged/changed.
# (The section sign U+00A7 is intentionally NOT here - it is a replacement target,
# converted to "Section " because it renders as garbage in many editors/previews.)
ALLOWLIST = {
    "\u00E9",  # e-acute (Draknare and other names)
}

# Mojibake markers: if any of these appear, a UTF-8 doc was almost certainly
# decoded as Windows-1252 somewhere. Common reversible cases are repaired by
# re-encoding latin-1 -> utf-8 on the offending run; otherwise just reported.
MOJIBAKE_MARKERS = ["\u00C3", "\u00E2\u20AC", "\u00C2"]

# Geometric / diagram / UI glyphs that are part of ASCII-art diagrams and are kept
# verbatim (NOT emoji, NOT converted). These appear inside box-drawing mockups as
# triangles, ballot boxes, squared operators, and diagram arrows. Box-drawing /
# block elements (U+2500..U+259F) are handled separately by is_box_drawing.
GEOMETRIC_KEEP = {
    "\u25A0",  # black square
    "\u25B2", "\u25B3",  # up triangles
    "\u25B6", "\u25B8", "\u25BA",  # right triangles (play / arrowhead)
    "\u25BC", "\u25BD", "\u25BE",  # down triangles
    "\u25C0", "\u25C4",  # left triangles
    "\u25EB",  # white square with vertical bisecting line
    "\u229E",  # squared plus
    "\u2610", "\u2611", "\u2612",  # ballot box / checked / with X (diagram checkboxes)
    "\u21B3",  # downwards arrow with tip rightwards (tree branch)
    "\u21C4",  # rightwards arrow over leftwards arrow (swap)
}

BOM = "\ufeff"


# --------------------------------------------------------------------------- #
# Core logic
# --------------------------------------------------------------------------- #

def is_box_drawing(ch: str) -> bool:
    """True for Box Drawing (U+2500..U+257F) and Block Elements (U+2580..U+259F).
    These are legitimate ASCII-art diagram content in this repo's docs, stored as
    clean UTF-8; we never flag or alter them."""
    cp = ord(ch)
    return 0x2500 <= cp <= 0x259F


def is_geometric(ch: str) -> bool:
    """True for diagram/UI glyphs we keep verbatim: box-drawing/block elements,
    plus the curated geometric shapes (triangles, ballot boxes, squared operators,
    diagram arrows) in GEOMETRIC_KEEP. These are part of ASCII art, not emoji."""
    return is_box_drawing(ch) or ch in GEOMETRIC_KEEP


def is_emoji(ch: str) -> bool:
    """True for pictographic emoji. These are KEPT verbatim - the user likes emoji
    and they render fine; they are never flagged or changed. This predicate exists
    so the classifier does not mistake emoji for "review" punctuation.

    Covers:
      - Astral-plane pictographs (U+1F000 and above): faces, objects, symbols,
        e.g. chart/clipboard/rocket/eye, stored as surrogate pairs.
      - The U+FE0F variation selector (the invisible 'render as emoji' suffix).
      - BMP 'Miscellaneous Symbols' / 'Dingbats' pictographs in U+2600..U+27BF
        (check mark, cross mark, warning sign, sparkles, gear, etc.).
      - A few media/arrow emoji outside that block (U+23E9..U+23FA media controls,
        U+2B05..U+2B07 arrows, U+2934/U+2935).

    Geometric / diagram glyphs (triangles, ballot boxes, squared operators) are
    classified separately by is_geometric (also kept).
    """
    if is_geometric(ch):
        return False
    cp = ord(ch)
    if cp >= 0x1F000:
        return True
    if cp == 0xFE0F:  # variation selector-16 ('emoji presentation')
        return True
    if 0x2600 <= cp <= 0x27BF:  # Misc Symbols + Dingbats (pictographs)
        return True
    if cp in (0x23E9, 0x23EA, 0x23EB, 0x23EC, 0x23ED, 0x23EE, 0x23EF,
              0x23F0, 0x23F1, 0x23F2, 0x23F3, 0x23F8, 0x23F9, 0x23FA,
              0x2B05, 0x2B06, 0x2B07, 0x2934, 0x2935):
        return True
    return False


def is_kept_nonascii(ch: str) -> bool:
    """True for non-ASCII we intentionally keep silently (neither converted nor
    reported): the allowlist (accented name letters), emoji/pictographs, geometric
    / diagram glyphs (box-drawing, triangles, ballot boxes), and any Unicode letter
    (other-language names). Everything else non-ASCII that is not the section sign
    or in DECORATIVE_REPLACEMENTS is surfaced as a "review" finding."""
    if ch in ALLOWLIST:
        return True
    if is_emoji(ch):
        return True
    if is_geometric(ch):
        return True
    cat = unicodedata.category(ch)
    # Letters (names) are fine.
    if cat.startswith("L"):
        return True
    return False


def try_repair_mojibake(text: str) -> str:
    """Attempt the classic 'utf-8 read as latin-1' repair. Only applied if it
    is a clean round-trip; otherwise the original text is returned unchanged."""
    if not any(m in text for m in MOJIBAKE_MARKERS):
        return text
    try:
        repaired = text.encode("latin-1").decode("utf-8")
    except (UnicodeEncodeError, UnicodeDecodeError):
        return text
    # Only accept the repair if it actually removed marker noise and did not
    # introduce replacement characters.
    if "\ufffd" in repaired:
        return text
    before = sum(text.count(m) for m in MOJIBAKE_MARKERS)
    after = sum(repaired.count(m) for m in MOJIBAKE_MARKERS)
    return repaired if after < before else text


def scan_text(text: str, aggressive: bool):
    """Return (findings, mojibake_present, bom_present, stray).
    findings: dict char -> count for chars that WILL be fixed: the section sign
              always, plus DECORATIVE_REPLACEMENTS when aggressive is set.
    stray: dict char -> count for other non-ASCII that is NOT kept (not emoji, not
           a diagram glyph, not a letter, not allowlisted) - surfaced for review.
    Decorative punctuation is kept silently unless aggressive (it renders fine).
    """
    bom_present = text.startswith(BOM)
    body = text[1:] if bom_present else text

    findings: dict[str, int] = {}
    stray: dict[str, int] = {}
    for ch in body:
        if ord(ch) < 0x80:
            continue
        if ch == SECTION_SIGN:
            findings[ch] = findings.get(ch, 0) + 1
        elif ch in DECORATIVE_REPLACEMENTS:
            if aggressive:
                findings[ch] = findings.get(ch, 0) + 1
            # else: renders fine in UTF-8-aware viewers; keep silently.
        elif not is_kept_nonascii(ch):
            stray[ch] = stray.get(ch, 0) + 1

    mojibake = any(m in body for m in MOJIBAKE_MARKERS)
    return findings, mojibake, bom_present, stray


def apply_fixes(text: str, strip_bom: bool, aggressive: bool) -> str:
    """Return fixed text: repair mojibake, always convert the section sign, and -
    only when aggressive - flatten decorative punctuation. Optional BOM strip.
    Emoji, diagram/geometric glyphs, and (by default) decorative punctuation are
    left untouched."""
    had_bom = text.startswith(BOM)
    body = text[1:] if had_bom else text
    body = try_repair_mojibake(body)
    # Section sign needs special spacing so "§5" -> "Section 5" and "§ 5" -> also
    # "Section 5" (collapse any run of spaces that already follows the sign).
    body = re.sub(r"\u00A7[ \t]*", "Section ", body)
    if aggressive:
        for bad, good in DECORATIVE_REPLACEMENTS.items():
            if bad in body:
                body = body.replace(bad, good)
    if had_bom and not strip_bom:
        return BOM + body
    return body


def iter_markdown_files(paths):
    """Yield .md files under the given paths (files or directories)."""
    seen = set()
    for p in paths:
        path = Path(p)
        if path.is_file() and path.suffix.lower() == ".md":
            rp = path.resolve()
            if rp not in seen:
                seen.add(rp)
                yield path
        elif path.is_dir():
            for md in sorted(path.rglob("*.md")):
                rp = md.resolve()
                if rp not in seen:
                    seen.add(rp)
                    yield md


def fmt_chars(d: dict[str, int]) -> str:
    parts = []
    for ch, n in sorted(d.items(), key=lambda kv: ord(kv[0])):
        parts.append("U+{:04X}(x{})".format(ord(ch), n))
    return ", ".join(parts)


# --------------------------------------------------------------------------- #
# CLI
# --------------------------------------------------------------------------- #

def main(argv=None) -> int:
    parser = argparse.ArgumentParser(
        description="Scan/auto-fix Markdown docs for funky characters and mojibake."
    )
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--check", action="store_true",
                      help="Report only; change nothing (default).")
    mode.add_argument("--fix", action="store_true",
                      help="Rewrite files in place (UTF-8, no BOM unless kept).")
    parser.add_argument("--aggressive", action="store_true",
                        help="Also convert decorative punctuation (dashes, arrows, "
                             "smart quotes, ellipsis, math) to ASCII. Off by default.")
    parser.add_argument("--strip-bom", action="store_true",
                        help="In --fix, also remove a UTF-8 BOM if present.")
    parser.add_argument("paths", nargs="*",
                        help="Files or folders to scan (default: repo Docs folders).")
    args = parser.parse_args(argv)

    do_fix = args.fix
    roots = args.paths if args.paths else DEFAULT_ROOTS

    files = list(iter_markdown_files(roots))
    if not files:
        print("No .md files found under: {}".format(", ".join(str(r) for r in roots)))
        return 2

    total_problem_files = 0
    total_fixed = 0

    for md in files:
        raw = md.read_bytes().decode("utf-8")  # bytes: preserve original CRLF/LF
        findings, mojibake, bom_present, stray = scan_text(raw, args.aggressive)
        has_problem = bool(findings) or mojibake or bom_present

        if do_fix:
            new = apply_fixes(raw, strip_bom=args.strip_bom, aggressive=args.aggressive)
            if new != raw:
                # Write bytes back as-is (no BOM unless the file kept one and the
                # user did not ask to strip it; original line endings preserved).
                md.write_bytes(new.encode("utf-8"))
                total_fixed += 1
                print("[fixed]  {}".format(md))
                if findings:
                    print("         replaced: {}".format(fmt_chars(findings)))
                if mojibake:
                    print("         repaired mojibake markers")
                if bom_present and args.strip_bom:
                    print("         stripped UTF-8 BOM")
            else:
                print("[ok]     {}".format(md))
        else:
            if has_problem:
                total_problem_files += 1
                print("[FUNKY]  {}".format(md))
                if findings:
                    print("         fix -> ASCII: {}".format(fmt_chars(findings)))
                if mojibake:
                    print("         mojibake markers present (Latin-1<->UTF-8 mismatch)")
                if bom_present:
                    print("         has UTF-8 BOM (repo convention is no BOM)")
            else:
                print("[ok]     {}".format(md))
            if stray:
                # Informational only; does not count as a failure.
                print("         note: other non-ASCII (review): {}".format(fmt_chars(stray)))

    print("-" * 60)
    if do_fix:
        print("Fixed {} of {} file(s).".format(total_fixed, len(files)))
        return 0
    if total_problem_files:
        print("{} of {} file(s) need attention. Re-run with --fix to repair."
              .format(total_problem_files, len(files)))
        return 1
    print("All {} file(s) clean.".format(len(files)))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
