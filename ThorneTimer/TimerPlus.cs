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
            if (string.IsNullOrEmpty(timeValue)) return 0;

            try
            {
                // Strip optional 'd' suffix so both "30 10:30:00" (input)
                // and "30d 10:30:00" (display) are handled uniformly.
                string normalized = timeValue.Replace("d ", " ");

                int spaceIdx = normalized.IndexOf(' ');
                if (spaceIdx > 0)
                {
                    // DD HH:MM:SS
                    int d = Convert.ToInt32(normalized.Substring(0, spaceIdx));
                    string[] parts = normalized.Substring(spaceIdx + 1).Split(':');
                    if (parts.Length == 3)
                    {
                        int h = Convert.ToInt32(parts[0]);
                        int m = Convert.ToInt32(parts[1]);
                        int s = Convert.ToInt32(parts[2]);
                        return new TimeSpan(d, h, m, s).TotalMilliseconds;
                    }
                }
                else
                {
                    // HH:MM:SS
                    string[] parts = normalized.Split(':');
                    if (parts.Length == 3)
                    {
                        int h = Convert.ToInt32(parts[0]);
                        int m = Convert.ToInt32(parts[1]);
                        int s = Convert.ToInt32(parts[2]);
                        return new TimeSpan(h, m, s).TotalMilliseconds;
                    }
                }
            }
            catch { }

            return 0;
        }
    }
}
