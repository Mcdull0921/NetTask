using System.ComponentModel;

namespace NetTaskManager
{
    public enum TimerType
    {
        [Description("不循环")]
        None = 0,
        [Description("分钟循环")]
        Minute = 1,
        [Description("小时循环")]
        Hour = 2,
        [Description("天循环")]
        Day = 3,
        [Description("月循环")]
        Month = 4,
        [Description("秒循环")]
        Second = 5
    }
}
