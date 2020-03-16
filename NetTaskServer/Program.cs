using NetTaskInterface;
using NetTaskManager;
using NetTaskServer.DB;
using NetTaskServer.HttpServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace NetTaskServer
{
    class Program
    {
        const string DB_PATH = "./user.db";
        const string LOG_PATH = "./Logs";
        static void Main(string[] args)
        {

            CancellationTokenSource ctsHttp = new CancellationTokenSource();
            IDbOperator DbOp = new LiteDbOperator(DB_PATH);//加载数据库
            var taskManager = TaskManager.Create();
            HttpServerAPIs api = new HttpServerAPIs(taskManager, DbOp, LOG_PATH);
            HttpServer.HttpServer httpServer = new HttpServer.HttpServer(taskManager.logger, api);
            var t = httpServer.StartHttpService(ctsHttp, 12315);

            Console.ReadKey();
        }

    }
}
