using System.ComponentModel;

namespace NetTaskManager
{
    public enum TimerType
    {
        [Description("非循环任务")]
        None = 0,
        [Description("分钟循环任务")]
        Minute = 1,
        [Description("小时循环任务")]
        Hour = 2,
        [Description("天循环任务")]
        Day = 3,
        [Description("月循环任务")]
        Month = 4,
        [Description("秒循环任务")]
        Second = 5
    }
}
