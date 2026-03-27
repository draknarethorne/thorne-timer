using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThorneTimer
{
    class Timers
    {
        public class GridData
        {
            public long ID { get; set; }
            public string Name { get; set; }
            public long CategoryID { get; set; }
            public string StartKeyword { get; set; }
            public string EndKeyword { get; set; }
            public string WAVFile { get; set; }
            public string Speech { get; set; }
            public string Duration { get; set; }
            public string Remaining { get; set; }
            public long ActiveYn { get; set; }
            public long CaseYn { get; set; }
            public long EndlessYn { get; set; }
        }

        static public string btnStart = "Start";
        static public string btnStop = "Stop";
        static public string btnPet = "Pet";
        static public string btnBuff = "Buff";
        static public string btnPing = "Ping";

        static public bool PetTimer(string btnString)
        {
            return (btnString == btnPet);
        }

        static public bool BuffTimer(string btnString)
        {
            return (btnString == btnBuff);
        }

        static public bool PingTimer(string btnString)
        {
            return (btnString == btnPing);
        }

        static public bool TimerStopped(string btnString)
        {
            return ((btnString == btnStart) || (btnString == null));
        }

        static public bool TimerRunning(string btnString)
        {
            return ((btnString == btnStop) || (btnString == btnBuff) || (btnString == btnPet));
        }


    }
}
