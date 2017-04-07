using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPLite
{
    public abstract class TaskBase
    {
        public int Rank { get; set; }
        public bool Enabled { get; set; }
        
        public bool ThisDayForwardOnly { get; set; }
    }

    public class DailyTask : TaskBase
    {

    }

    public class TaskGroup
    {
        // Multiple daily tasks
    }
}
