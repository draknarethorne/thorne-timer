using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThorneTimer
{
    public class Characters
    {
        public class GridData
        {
            public long ID { get; set; }
            public string Name { get; set; }
            public string LogFile { get; set; }
            public int MiniViewX { get; set; }
            public int MiniViewY { get; set; }
        }

        public Characters()
        {
        }
    }
}