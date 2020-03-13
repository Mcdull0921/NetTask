using System.ComponentModel;

namespace NetTaskManager
{
    public enum TaskStatus
    {
        [Description("停止")]
        Stop,
        [Description("运行中")]
        Running,
        [Description("等待运行")]
        Waitting,
        [Description("等待停止")]
        WaittingForStop
    }
}
