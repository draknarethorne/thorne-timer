using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ThorneTimer
{
    public class TimerPlus : Timer
    {
        public enum TimerType
        {
            Ping,
            Pet,
            Buff,
            Normal
        }

        public long TimerID = 0;
        public double ElapsedTime = 0;
        public double DurationTime = 0;
        public TimerType TheType = TimerType.Normal;

        public class TimerPlusEventArgs : EventArgs
        {
            public long TimerID = 0;
            public double ElapsedTime = 0;
            public double Duration = 0;
        }

        public event EventHandler<TimerPlus> TimerExpired;
        public event EventHandler<TimerPlus> TimerElapsed;

        public TimerPlus() : base()
        {
            this.Elapsed += this.ElapsedAction;
        }

        private void ElapsedAction(object sender, ElapsedEventArgs e)
        {
            if (this.AutoReset)
            {
                this.ElapsedTime += this.Interval;

                TimerPlus ea = new TimerPlus
                {
                    TimerID = this.TimerID,
                    ElapsedTime = this.ElapsedTime,
                    DurationTime = this.DurationTime,
                    TheType = this.TheType
                };

                EventHandler<TimerPlus> evt1 = TimerElapsed;
                evt1(this, ea);

                if (this.ElapsedTime >= this.DurationTime)
                {
                    EventHandler<TimerPlus> evt2 = TimerExpired;
                    evt2(this, ea);
                }
            }
        }

        public string GetTimeRemaining()
        {
            return GetTimeRemaining(TimeFormat.Classic);
        }

        public string GetTimeRemaining(TimeFormat format)
        {
            TimeSpan t = TimeSpan.FromMilliseconds(this.DurationTime - this.ElapsedTime);

            return TimerTimeFormatter.Format(t, format);
        }

        static public double GetMilliseconds(string timeValue)
        {
            return TryParseRemaining(timeValue, out double ms) ? ms : 0;
        }

        /// <summary>
        /// Parses a time string into milliseconds, tolerant of every format produced
        /// by <see cref="TimerTimeFormatter"/> as well as the raw user/wire input forms.
        /// Returns <c>false</c> (and <paramref name="milliseconds"/> = 0) when the text
        /// cannot be understood, so callers can distinguish a real zero from a parse
        /// failure instead of treating both as 0 (the bug behind dependency delays and
        /// timer restore silently mis-firing under non-Classic style formats).
        ///
        /// Supported:
        ///   â€¢ Colon forms (Classic / Long):  "01:23:45", "1:23:45", "0:45",
        ///     "30 10:30:00" / "30d 04:05:22"  (leading days, with or without 'd').
        ///   â€¢ Unit-suffixed forms (AdaptiveCompact / FullCompact):  "45s", "23m 45s",
        ///     "1h 23m", "1h 0m 5s", "1d 4h", "1d 4h 5m 22s".
        /// Note AdaptiveCompact is lossy (keeps only the two top units), so a restored
        /// value may round to that granularity â€” acceptable for display, which is why
        /// runtime restore persists Classic (see SaveCharacterState / restore paths).
        /// </summary>
        static public bool TryParseRemaining(string timeValue, out double milliseconds)
        {
            milliseconds = 0;
            if (string.IsNullOrWhiteSpace(timeValue)) return false;

            string trimmed = timeValue.Trim();

            // Unit-suffixed forms contain at least one of the d/h/m/s letters but no ':'.
            // (Days in a colon form appear as a leading number, not a trailing letter.)
            if (trimmed.IndexOf(':') < 0 &&
                trimmed.IndexOfAny(new[] { 'd', 'h', 'm', 's' }) >= 0)
            {
                return TryParseUnitSuffixed(trimmed, out milliseconds);
            }

            return TryParseColon(trimmed, out milliseconds);
        }

        /// <summary>Parses colon forms, with an optional leading "{days}" or "{days}d".</summary>
        static private bool TryParseColon(string text, out double milliseconds)
        {
            milliseconds = 0;

            try
            {
                // Strip optional 'd' suffix so both "30 10:30:00" (input)
                // and "30d 10:30:00" (display) are handled uniformly.
                string normalized = text.Replace("d ", " ");

                int spaceIdx = normalized.IndexOf(' ');
                if (spaceIdx > 0)
                {
                    // DD HH:MM:SS
                    if (!int.TryParse(normalized.Substring(0, spaceIdx), out int d)) return false;
                    string[] parts = normalized.Substring(spaceIdx + 1).Split(':');
                    if (parts.Length == 3
                        && int.TryParse(parts[0], out int h)
                        && int.TryParse(parts[1], out int m)
                        && int.TryParse(parts[2], out int s))
                    {
                        milliseconds = new TimeSpan(d, h, m, s).TotalMilliseconds;
                        return true;
                    }
                }
                else
                {
                    // HH:MM:SS or M:SS / MM:SS (Long collapses the hours slot under an hour)
                    string[] parts = normalized.Split(':');
                    if (parts.Length == 3
                        && int.TryParse(parts[0], out int h)
                        && int.TryParse(parts[1], out int m)
                        && int.TryParse(parts[2], out int s))
                    {
                        milliseconds = new TimeSpan(h, m, s).TotalMilliseconds;
                        return true;
                    }
                    if (parts.Length == 2
                        && int.TryParse(parts[0], out int m2)
                        && int.TryParse(parts[1], out int s2))
                    {
                        milliseconds = new TimeSpan(0, 0, m2, s2).TotalMilliseconds;
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        /// <summary>
        /// Parses unit-suffixed forms ("1d 4h 5m 22s", "23m 45s", "45s"). Each token is
        /// a number immediately followed by a single d/h/m/s unit; tokens are
        /// whitespace-separated and ordered most- to least-significant.
        /// </summary>
        static private bool TryParseUnitSuffixed(string text, out double milliseconds)
        {
            milliseconds = 0;

            string[] tokens = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) return false;

            long days = 0, hours = 0, minutes = 0, seconds = 0;
            bool any = false;

            foreach (string token in tokens)
            {
                char unit = token[token.Length - 1];
                string numberPart = token.Substring(0, token.Length - 1);
                if (!long.TryParse(numberPart, out long value)) return false;

                switch (unit)
                {
                    case 'd': days = value; break;
                    case 'h': hours = value; break;
                    case 'm': minutes = value; break;
                    case 's': seconds = value; break;
                    default: return false;
                }
                any = true;
            }

            if (!any) return false;

            milliseconds = ((((days * 24 + hours) * 60 + minutes) * 60 + seconds)) * 1000.0;
            return true;
        }
    }
}
