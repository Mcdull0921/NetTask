using NetTaskManager;
using NetTaskServer.DB;
using NetTaskServer.HttpServer;
using PeterKottas.DotNetCore.WindowsService.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace NetTaskServer
{
    class ServerHost
    {
        const string DB_PATH = "./user.db";
        const string LOG_PATH = "./Logs";
        static TaskManager taskManager;
        public void Start(int port)
        {
            CancellationTokenSource ctsHttp = new CancellationTokenSource();
            IDbOperator DbOp = new LiteDbOperator(DB_PATH);//加载数据库
            taskManager = TaskManager.Create();
            HttpServerAPIs api = new HttpServerAPIs(taskManager, DbOp, LOG_PATH);
            HttpServer.HttpServer httpServer = new HttpServer.HttpServer(taskManager.logger, api);
            var _ = httpServer.StartHttpService(ctsHttp, port);
        }

        public void Stop()
        {

            taskManager.StopAllTask();
            int retry = 100;       //等待所有任务结束或者等待超时
            while (taskManager.Tasks.Count(p => p.Status != TaskStatus.Stop) > 0 && retry > 0)
            {
                --retry;
                System.Threading.Thread.Sleep(500);
            }
            Environment.Exit(0);
        }
    }
}
