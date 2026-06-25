using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThorneTimer
{
    public class Categories
    {
        public class GridData
        {
            public long ID { get; set; }
            public string Name { get; set; }
            public string StartKeyword { get; set; }
            public string EndKeyword { get; set; }
            public long AutoStop { get; set; }
        }
    }
}

