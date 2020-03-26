using NetTaskInterface;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace NetTaskManager
{
    public class TaskManager
    {
        const int DELAY_SECOND = 2;
        const int MONITOR_RATE = 500;

        private static TaskManager singleton = new TaskManager();
        private readonly object lockObject = new object();    //操作queue的地方都要锁住保证线程同步
        public readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public static TaskManager Create()
        {
            return singleton;
        }

        Dictionary<Guid, TaskAgent> tasks = new Dictionary<Guid, TaskAgent>();
        Dictionary<Assembly, string> assemblies = new Dictionary<Assembly, string>();
        Dictionary<Guid, TaskProcess> queue;
        Task thread;
        CancellationTokenSource cts;
        CancellationToken token;

        private TaskManager()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            queue = new Dictionary<Guid, TaskProcess>();
            if (!Directory.Exists(AssemblyPath))
            {
                Directory.CreateDirectory(AssemblyPath);
            }
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (assemblies.ContainsKey(args.RequestingAssembly))
            {
                string dllPath = Path.Join(AssemblyPath, assemblies[args.RequestingAssembly], args.Name.Remove(args.Name.IndexOf(',')) + ".dll");
                if (!File.Exists(dllPath))
                    throw new FileNotFoundException("未能找到依赖项" + args.Name);
                byte[] assemblyInfo = File.ReadAllBytes(dllPath);
                var assembly = Assembly.Load(assemblyInfo);
                assemblies.Add(assembly, assemblies[args.RequestingAssembly]);
                return assembly;
            }
            return null;
        }

        void CheckQueue()
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                lock (lockObject)
                {
                    List<TaskProcess> popItems = new List<TaskProcess>();
                    var time = DateTime.Now.AddSeconds(DELAY_SECOND); //增加延迟确保执行时间更加准确
                    foreach (var t in queue.Values.Where(p => p.processTime <= time).OrderBy(p => p.processTime))
                    {
                        popItems.Add(t);
                    }
                    foreach (var t in popItems)
                    {
                        queue.Remove(t.Id);
                        Process(t);
                    }
                }
                Thread.Sleep(MONITOR_RATE);
            }
        }

        void Process(TaskProcess tp)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(obj =>
            {
                if (tp.processTime > DateTime.Now)
                {
                    var ts = tp.processTime - DateTime.Now;
                    Thread.Sleep(ts);
                }
                var task = tp.task;
                task.Process();
                if (task.Status == TaskStatus.Waitting && task.NextProcessTime.HasValue)
                    AddProcessQueue(task, task.NextProcessTime.Value);
            }));
        }

        internal void AddProcessQueue(TaskAgent task, DateTime processTime)
        {
            if (task.Status != TaskStatus.Waitting)
                return;
            lock (lockObject)
            {
                if (queue.ContainsKey(task.Id))
                {
                    queue.Remove(task.Id);
                }
                var process = new TaskProcess() { task = task, processTime = processTime };
                queue.Add(process.Id, process);
            }
        }

        void ClearQueue()
        {
            lock (lockObject)
            {
                List<TaskProcess> popItems = new List<TaskProcess>();
                foreach (var t in queue.Values.Where(p => p.task.Status == TaskStatus.Stop))
                {
                    popItems.Add(t);
                }
                foreach (var t in popItems)
                {
                    queue.Remove(t.Id);
                }
            }
        }

        bool AddTask(TaskAgent task)
        {
            if (tasks.ContainsKey(task.Id))
                return false;
            tasks.Add(task.Id, task);
            return true;
        }

        public string AssemblyPath
        {
            get
            {
                return Path.Join(new FileInfo(Assembly.GetEntryAssembly().Location).Directory.FullName, "Assembly");
            }
        }

        private Dictionary<string, TaskRunParam> LoadTaskRunParam(Guid assemblyId)
        {
            var path = Path.Join(AssemblyPath, assemblyId.ToString() + ".json");
            if (!File.Exists(path))
                return null;
            var res = new Dictionary<string, TaskRunParam>();
            using (StreamReader sr = new StreamReader(path, Encoding.UTF8))
            {
                var configs = JsonConvert.DeserializeObject<IEnumerable<TaskRunParam>>(sr.ReadToEnd());
                foreach (var c in configs)
                {
                    res.Add(c.taskTypeName, c);
                }
            }
            return res;
        }

        private void SaveTaskRunParam(Guid assemblyId, IEnumerable<TaskRunParam> configs)
        {
            var path = Path.Join(AssemblyPath, assemblyId.ToString() + ".json");
            var content = JsonConvert.SerializeObject(configs);
            using (StreamWriter sw = new StreamWriter(path, false, Encoding.UTF8))
            {
                sw.Write(content);
            }
        }

        public void LoadAssembly(Guid assemblyId)
        {
            var rootDir = Path.Join(AssemblyPath, assemblyId.ToString());
            string xmlPath = Path.Join(rootDir, "main.xml");
            if (!File.Exists(xmlPath))
                throw new FileNotFoundException("入口文件main.xml未找到！");
            var configuration = new Configuration(xmlPath);
            var dllPath = Path.Join(rootDir, configuration.EntryPoint);
            if (!File.Exists(dllPath))
                throw new FileNotFoundException(string.Format("程序集{0}文件未找到！", configuration.EntryPoint));
            byte[] assemblyInfo = File.ReadAllBytes(dllPath);
            var assembly = Assembly.Load(assemblyInfo);
            assemblies.Add(assembly, assemblyId.ToString());
            var configs = LoadTaskRunParam(assemblyId);
            bool saveConfig = configs == null;
            List<TaskRunParam> saveConfigs = new List<TaskRunParam>();
            var taskTypes = assembly.GetExportedTypes().Where(t => t.IsClass && !t.IsAbstract && typeof(NetTaskInterface.ITask).IsAssignableFrom(t)).ToArray();
            foreach (var t in taskTypes)
            {
                ITask task = (ITask)Activator.CreateInstance(t);
                var typeName = task.GetType().FullName;
                TaskRunParam config = null;
                if (configs != null && configs.ContainsKey(typeName))
                    config = configs[typeName];
                else
                    config = TaskRunParam.CreateDefaultConfig(typeName);
                TaskAgent ta = new TaskAgent(task, assemblyId, config);
                ta.configuration = configuration;
                AddTask(ta);
                if (saveConfig)
                    saveConfigs.Add(config);
            }
            if (saveConfig)
                SaveTaskRunParam(assemblyId, saveConfigs);
        }


        public void ReloadAssembly()
        {
            if (Tasks.Count(t => t.Status != TaskStatus.Stop) > 0)
                throw new ApplicationException("有任务尚未结束，无法重新加载程序集");
            ClearQueue();
            tasks.Clear();
            assemblies.Clear();
            foreach (var dir in Directory.GetDirectories(AssemblyPath))
            {
                DirectoryInfo di = new DirectoryInfo(dir);
                var gid = new Guid(di.Name);
                try
                {
                    LoadAssembly(gid);
                }
                catch (FileNotFoundException ex)
                {
                    logger.Error("程序集加载错误", ex);
                }
            }
            Start();
        }

        public void DeleteAssembly(Guid assemblyId)
        {
            if (Tasks.Count(t => t.AssemblyId == assemblyId && t.Status != TaskStatus.Stop) > 0)
                throw new TaskNotStopException();
            ClearQueue();
            var deleteIds = Tasks.Where(t => t.AssemblyId == assemblyId).Select(t => t.Id).ToArray();
            foreach (var id in deleteIds)
            {
                tasks.Remove(id);
            }
            var assemblyPath = Path.Join(AssemblyPath, assemblyId.ToString());
            if (Directory.Exists(assemblyPath))
                Directory.Delete(assemblyPath, true);
            assemblyPath += ".json";
            if (File.Exists(assemblyPath))
                File.Delete(assemblyPath);
        }

        private bool Start()
        {
            if (IsRunning)
                return false;
            cts = new CancellationTokenSource();
            token = cts.Token;
            thread = Task.Run(new Action(CheckQueue));
            //StartAllTask();
            logger.Info("任务管理器重新加载程序集");
            return true;
        }

        //private bool Stop()
        //{
        //    if (!IsRunning)
        //        return false;
        //    StopAllTask();
        //    cts.Cancel();
        //    int retry = 30;
        //    while (retry > 0)
        //    {
        //        if (thread.Status == System.Threading.Tasks.TaskStatus.Faulted)
        //        {
        //            thread = null;
        //            logger.Info("任务管理器停止运行");
        //            return true;
        //        }
        //        --retry;
        //        Thread.Sleep(100);
        //    }
        //    return false;
        //}

        public void StartAllTask()
        {
            if (!IsRunning)
                return;
            foreach (var t in tasks.Values.Where(p => p.Status == TaskStatus.Stop))
            {
                t.Start(this);
            }
        }

        public void StopAllTask()
        {
            foreach (var t in tasks.Values)
            {
                t.Stop();
            }
            ClearQueue();
        }

        public void StartTask(Guid id)
        {
            if (!IsRunning)
                return;
            if (!tasks.ContainsKey(id))
                throw new TaskNotExistException();
            if (tasks[id].Status != TaskStatus.Stop)
                throw new TaskNotStopException();
            tasks[id].Start(this);
        }

        public void StopTask(Guid id)
        {
            if (!tasks.ContainsKey(id))
                throw new TaskNotExistException();
            tasks[id].Stop();
            ClearQueue();
        }

        public void RunImmediatelyTask(Guid id)
        {
            if (!IsRunning)
                return;
            if (!tasks.ContainsKey(id))
                throw new TaskNotExistException();
            if (tasks[id].Status != TaskStatus.Stop)
                throw new TaskNotStopException();
            tasks[id].RunImmediately(this);
        }

        public void EditTaskConfig(Guid id, params KeyValuePair<string, string>[] configs)
        {
            if (!tasks.ContainsKey(id))
                throw new TaskNotExistException();
            if (tasks[id].Status != TaskStatus.Stop)
                throw new TaskNotStopException();
            if (configs == null)
                return;
            var t = tasks[id];
            XmlDocument doc = new XmlDocument();
            var root = doc.CreateElement("task");
            root.SetAttribute("entrypoint", t.configuration.EntryPoint);
            foreach (var c in configs)
            {
                XmlElement node = doc.CreateElement("add");
                node.SetAttribute("key", c.Key);
                node.SetAttribute("value", c.Value);
                root.AppendChild(node);
            }
            doc.AppendChild(root);
            var path = t.configuration.Path;
            doc.Save(path);
            t.configuration = new Configuration(path);
        }

        public void EditTaskRunParam(Guid id, TimerType timerType, int interval, DateTime? startTime, bool runOnStart)
        {
            if (!tasks.ContainsKey(id))
                throw new TaskNotExistException();
            if (tasks[id].Status != TaskStatus.Stop)
                throw new TaskNotStopException();
            var t = tasks[id];
            var configs = LoadTaskRunParam(t.AssemblyId);
            var config = configs[t.TypeName];
            config.interval = interval;
            config.timerType = timerType;
            config.startTime = startTime;
            config.runOnStart = runOnStart;
            t.runParam = config;
            SaveTaskRunParam(t.AssemblyId, configs.Values);
        }

        public IEnumerable<TaskAgent> Tasks
        {
            get
            {
                return tasks.Values;
            }
        }

        public bool IsRunning
        {
            get
            {
                return thread != null;
            }
        }

        class TaskProcess
        {
            public TaskAgent task { get; set; }
            public DateTime processTime { get; set; }
            public Guid Id { get { return task.Id; } }
        }
    }
}
