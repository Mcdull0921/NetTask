using NetTaskInterface;
using NetTaskManager;
using NetTaskServer.DB;
using NetTaskServer.HttpServer;
using PeterKottas.DotNetCore.WindowsService;
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
        const string Service_Name = "NetTask";
        const string Service_DisplayName = "NetTask";
        const string Service_Description = "基于Web的任务管理系统";
        const int Default_Port = 12315;
        static void Main(string[] args)
        {
            ServiceRunner<ServerHost>.Run(config =>
            {
                config.SetDisplayName(Service_Name);
                config.SetName(Service_DisplayName);
                config.SetDescription(Service_Description);

                config.Service(serviceConfig =>
                {
                    serviceConfig.ServiceFactory((extraArguments, controller) =>
                    {
                        return new ServerHost();
                    });

                    serviceConfig.OnStart((service, extraParams) =>
                    {
                        int port = Default_Port;
                        if (extraParams.Count > 0 && int.TryParse(extraParams[0], out int p))
                        {
                            port = p;
                        }
                        service.Start(port);
                        Console.WriteLine("Service {0} started at port:{1}", Service_Name, port);
                    });

                    serviceConfig.OnStop(service =>
                    {
                        service.Stop();
                        Console.WriteLine("Service {0} stopped", Service_Name);
                    });

                    serviceConfig.OnError(e =>
                    {
                        Console.WriteLine("Service {0} errored with exception : {1}", Service_Name, e.Message);
                    });
                });


            });
        }

    }
}
