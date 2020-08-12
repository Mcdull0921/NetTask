using NetTaskManager;
using NetTaskServer.Common;
using NetTaskServer.DB;
using NetTaskServer.DB.Model;
using NetTaskServer.HttpServer;
using Newtonsoft.Json;
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
        const string USER_DB_PATH = "./user.db";
        const string SYS_DB_PATH = "./sys.db";
        const string LOG_PATH = "./Logs";
        static TaskManager taskManager;
        static IDbOperator userDB;
        static IDbOperator sysDB;
        public void Start(int port)
        {
            CancellationTokenSource ctsHttp = new CancellationTokenSource();
            userDB = new LiteDbOperator(USER_DB_PATH);//加载数据库
            sysDB = new LiteDbOperator(SYS_DB_PATH);
            taskManager = TaskManager.Create();
            taskManager.OnTaskError += TaskManager_OnTaskError;
            taskManager.OnTaskMail += TaskManager_OnTaskMail;
            HttpServerAPIs api = new HttpServerAPIs(taskManager, userDB, sysDB, LOG_PATH);
            HttpServer.HttpServer httpServer = new HttpServer.HttpServer(taskManager.logger, api);
            var _ = httpServer.StartHttpService(ctsHttp, port);
        }

        private void TaskManager_OnTaskMail(TaskAgent sender, string content, string receiver)
        {
            Mail($"来自任务({sender.Name}-{sender.TypeName})的消息", (s) => content, receiver);
        }

        private void TaskManager_OnTaskError(TaskAgent sender, Exception exception)
        {
            Mail($"任务({sender.Name}-{sender.TypeName})发生异常已停止", (s) => s.Replace("${error}", exception.Message));
        }

        private void Mail(string title, Func<string, string> replace, string receiver = null)
        {
            var value = sysDB.Get(SystemKey.EMAIL.ToString());
            if (!string.IsNullOrEmpty(value))
            {
                var mail = JsonConvert.DeserializeObject<MailAccount>(value);
                if (mail.enable)
                {
                    EmailHelper emailHelper = new EmailHelper(mail.smtpServer, mail.smtpPort, mail.userName, mail.password);
                    string content = replace(mail.content);
                    if (!string.IsNullOrEmpty(receiver))
                    {
                        emailHelper.SendEmail(receiver, title, content, taskManager.logger);
                    }
                    else
                    {
                        var users = userDB.Select(0, 10).Select(p => JsonConvert.DeserializeObject<User>(p));
                        foreach (var u in users)
                        {
                            if (u.receiveEmail && !string.IsNullOrEmpty(u.email))
                            {
                                emailHelper.SendEmail(u.email, title, content, taskManager.logger);
                            }
                        }
                    }
                }
            }
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
