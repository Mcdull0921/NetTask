using NetTaskInterface;
using System;

namespace TestTask1
{
    public class Class1 : ITask
    {

        public override string name => "Test1";

        public override void process()
        {
            var t = new TestLibrary.Test();
            t.Hello("TestTask1");
            logger.Info("Info Test");
            Console.WriteLine(configuration["a"]);
            Console.WriteLine(configuration.GetIntValue("b"));
        }
    }
}
