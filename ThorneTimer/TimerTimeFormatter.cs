using System;
using System.Text;

namespace ThorneTimer
{
    /// <summary>
    /// Per-style time display format. Stored as an <c>int</c> on the
    /// <c>styles</c> table (<c>TimeFormat</c> column). <see cref="Classic"/> is
    /// the default and reproduces the original <c>TimerPlus.GetTimeRemaining()</c>
    /// output byte-for-byte so existing <c>.tdb</c> files render unchanged.
    /// </summary>
    public enum TimeFormat
    {
        Classic = 0,          // 1d 04:05:22 / 01:23:45  (current behavior, always zero-padded)
        Long = 1,             // 1d 4:05:22 / 1:23:45 / 0:45  (collapses, right-justified)
        AdaptiveCompact = 2,  // 1d 4h / 1h 23m / 45s  (two most-significant units)
        FullCompact = 3       // 1d 4h 5m 22s / 1h 23m 45s / 45s  (every non-zero unit)
    }

    /// <summary>
    /// Single source of truth for rendering a remaining-time <see cref="TimeSpan"/>
    /// as display text. Both <c>TimerPlus.GetTimeRemaining()</c> and the main grid
    /// Remaining column call into <see cref="Format(TimeSpan, TimeFormat)"/> so the
    /// mini view, the grid, and the Styles-tab preview all agree.
    /// See <c>Docs/styles-and-views-enhancements.md</c> §2.
    /// </summary>
    public static class TimerTimeFormatter
    {
        /// <summary>
        /// Formats <paramref name="span"/> using the supplied <paramref name="format"/>.
        /// Negative spans are clamped to zero (mirrors the original behavior of never
        /// counting below the duration once expired in practice).
        /// </summary>
        public static string Format(TimeSpan span, TimeFormat format)
        {
            if (span < TimeSpan.Zero) span = TimeSpan.Zero;

            switch (format)
            {
                case TimeFormat.Long:
                    return FormatLong(span);
                case TimeFormat.AdaptiveCompact:
                    return FormatAdaptiveCompact(span);
                case TimeFormat.FullCompact:
                    return FormatFullCompact(span);
                case TimeFormat.Classic:
                default:
                    return FormatClassic(span);
            }
        }

        /// <summary>
        /// Classic — always full zero-padded <c>HH:MM:SS</c>, with an optional
        /// leading <c>{Days}d</c>. Exact match of the original output.
        /// </summary>
        private static string FormatClassic(TimeSpan t)
        {
            if (t.Days > 0)
                return string.Format("{0}d {1:00}:{2:00}:{3:00}", t.Days, t.Hours, t.Minutes, t.Seconds);

            return string.Format("{0:00}:{1:00}:{2:00}", t.Hours, t.Minutes, t.Seconds);
        }

        /// <summary>
        /// Long — collapse to the most-significant non-zero unit, drop that unit's
        /// zero-padding, keep lower units two-digit padded. Under an hour the hours
        /// slot collapses away (<c>0:45</c>, <c>12:05</c>).
        /// </summary>
        private static string FormatLong(TimeSpan t)
        {
            if (t.Days > 0)
                return string.Format("{0}d {1}:{2:00}:{3:00}", t.Days, t.Hours, t.Minutes, t.Seconds);

            if (t.Hours > 0)
                return string.Format("{0}:{1:00}:{2:00}", t.Hours, t.Minutes, t.Seconds);

            return string.Format("{0}:{1:00}", t.Minutes, t.Seconds);
        }

        /// <summary>
        /// Adaptive Compact — the two most-significant non-zero units, largest first
        /// (<c>1d 4h</c>, <c>1h 23m</c>, <c>23m 45s</c>, <c>45s</c>).
        /// </summary>
        private static string FormatAdaptiveCompact(TimeSpan t)
        {
            if (t.Days > 0)
                return string.Format("{0}d {1}h", t.Days, t.Hours);

            if (t.Hours > 0)
                return string.Format("{0}h {1}m", t.Hours, t.Minutes);

            if (t.Minutes > 0)
                return string.Format("{0}m {1}s", t.Minutes, t.Seconds);

            return string.Format("{0}s", t.Seconds);
        }

        /// <summary>
        /// Full Compact — drop leading zero units, then show every unit from the
        /// most-significant non-zero unit down to seconds (including intermediate
        /// and trailing zeros). This keeps a counting-down timer continuous, e.g.
        /// <c>4m 0s</c> instead of a bare <c>4m</c>, and <c>1h 0m 5s</c>.
        /// </summary>
        private static string FormatFullCompact(TimeSpan t)
        {
            int totalDays = (int)t.TotalDays;
            var sb = new StringBuilder();

            if (totalDays > 0) Append(sb, totalDays, "d");
            if (t.Hours > 0 || sb.Length > 0) Append(sb, t.Hours, "h");
            if (t.Minutes > 0 || sb.Length > 0) Append(sb, t.Minutes, "m");
            Append(sb, t.Seconds, "s");

            return sb.ToString();
        }

        private static void Append(StringBuilder sb, int value, string suffix)
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(value);
            sb.Append(suffix);
        }
    }
}
