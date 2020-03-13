using NetTaskInterface;
using System;

namespace TestTask2
{
    public class Class1 : ITask
    {

        public override string name => "Test2";

        public override void process()
        {
            var t = new TestLibrary.Test();
            t.Hello("TestTask2");
            logger.Info("Info Test2");
            Console.WriteLine(configuration["a"]);
            Console.WriteLine(configuration.GetIntValue("b"));
        }
    }
}
