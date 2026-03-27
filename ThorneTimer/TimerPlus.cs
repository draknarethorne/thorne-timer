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

        public int RowIndex = 0;
        public double ElapsedTime = 0;
        public double DurationTime = 0;
        public TimerType TheType = TimerType.Normal;

        public class TimerPlusEventArgs : EventArgs
        {
            public int RowIndex = 0;
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
                    RowIndex = this.RowIndex,
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
            TimeSpan t = TimeSpan.FromMilliseconds(this.DurationTime - this.ElapsedTime);

            return String.Format("{0:00}:{1:00}:{2:00}", t.Hours, t.Minutes, t.Seconds);
        }

        static public double GetMilliseconds(string timeValue)
        {
            double ms = 0;

            try
            {
                int hours = Convert.ToInt32(timeValue.Substring(0, 2));
                int minutes = Convert.ToInt32(timeValue.Substring(3, 2));
                int seconds = Convert.ToInt32(timeValue.Substring(6, 2));

                DateTime dt1 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hours, minutes, seconds);
                DateTime dt2 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);

                ms = (dt1 - dt2).TotalMilliseconds;
            }
            catch { }

            return ms;
        }
    }
}
