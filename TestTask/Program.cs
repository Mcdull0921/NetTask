using System;

namespace TestTask
{
    class Program
    {
        static void Main(string[] args)
        {
            TestTask1.Class1 task = new TestTask1.Class1();
            task.process();

            Console.ReadKey();
        }
    }
}
