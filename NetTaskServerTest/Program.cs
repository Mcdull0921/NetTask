using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NetTaskManager;

namespace NetTaskServerTest
{
    class Program
    {
        static void Main(string[] args)
        {
            TaskManager taskManager = TaskManager.Create();

            taskManager.ReloadAssembly();

            foreach (var t in taskManager.Tasks)
            {
                Console.WriteLine("{0}\t{1}", t.Name, t.Status);
            }

            var id = taskManager.Tasks.ElementAt(0).Id;
            taskManager.EditTaskConfig(id, new KeyValuePair<string, string>("a", "123"), new KeyValuePair<string, string>("b", "456"));


            Console.WriteLine("{0}  开始启动任务", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ffff"));
            taskManager.StartAllTask();



            Thread.Sleep(10000);

            taskManager.StopAllTask();
            Console.WriteLine("{0}  结束任务", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ffff"));

            Thread.Sleep(2000);

            Console.WriteLine("{0}  重新开始启动任务", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ffff"));

            taskManager.StartAllTask();

            Console.ReadKey();

        }
    }
}
