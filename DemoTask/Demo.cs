using NetTaskInterface;
using System;

namespace DemoTask
{
    public class Demo : ITask
    {
        public override string name => "测试任务";

        public override void process()
        {
            logger.Info("Hello," + configuration["name"]);
        }
    }
}
