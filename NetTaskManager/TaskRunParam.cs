using System;
using System.Collections.Generic;
using System.Text;

namespace NetTaskManager
{
    class TaskRunParam
    {
        public string taskTypeName { get; set; }
        public int interval { get; set; }
        public TimerType timerType { get; set; }
        public DateTime? startTime { get; set; }
        public bool runOnStart { get; set; }
        public static TaskRunParam CreateDefaultConfig(string taskTypeName)
        {
            return new TaskRunParam()
            {
                taskTypeName = taskTypeName,
                interval = 0,
                timerType = TimerType.None,
                startTime = null,
                runOnStart = false,
            };
        }
    }
}
