using NetTaskManager;
using NetTaskServer.Common;
using NetTaskServer.DB;
using NetTaskServer.DB.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using NetTaskServer.Data;

namespace NetTaskServer.HttpServer
{
    class HttpServerAPIs
    {
        public const string SUPER_VARIABLE_INDEX_ID = "$index_id$";
        private const string MAIN_MODULE = "NetTaskManager.TaskManager";
        private const string INTERFACE_DLL = "NetTaskInterface.dll";

        private TaskManager ServerContext;
        private IDbOperator Dbop;
        private string baseLogFilePath;

        public HttpServerAPIs(TaskManager serverContext, IDbOperator dbOperator, string logfilePath)
        {
            ServerContext = serverContext;
            Dbop = dbOperator;
            baseLogFilePath = logfilePath;

            //如果库中没有任何记录，则增加默认用户
            if (Dbop.GetLength() < 1)
            {
                AddUserV2("admin", "admin", "2");
            }

            serverContext.ReloadAssembly();
            serverContext.StartAllTask();
        }

        #region 登录
        [FormAPI]
        public string Login(string username, string userpwd, string ip)
        {
            //1.校验
            dynamic user = Dbop.Get(username)?.ToDynamic();
            if (user == null)
            {
                return "错误: 用户不存在。请点击<a href='javascript:history.go(-1)'>此处</a>返回。";
            }


            if (user.userPwd != EncryptHelper.SHA256(userpwd))
            {
                return "错误: 密码不正确。请点击<a href='javascript:history.go(-1)'>此处</a>返回。";
            }
            ServerContext.logger.Info($"用户{username}登录成功，IP：{ip}");
            //2.给token
            string output = $"{username}|{DateTime.Now.ToString("yyyy-MM-dd")}|{user["role"].Value}";
            string token = EncryptHelper.AES_Encrypt(output);
            return string.Format(@"
<html>
<head><script>
document.cookie='NSPTK={0}; path=/;';
document.cookie='ROLE={1}; path=/;';
document.cookie='UNAME={2}; path=/;';
document.write('Redirecting...');
window.location.href='main.html';
</script>
</head>
</html>
            ", token, user["role"].Value, username);
        }
        #endregion

        #region 用户
        [API]
        [Secure(2)]
        public List<string> GetUsers()
        {
            List<string> userStrList = Dbop.Select(0, 999);
            return userStrList;
        }

        [API]
        [Secure(2), LoginInfo]
        public void AddUserV2(string userName, string userpwd, string role, LoginInfo info = null)
        {

            if (Dbop.Exist(userName))
            {
                throw new Exception("error: user exist.");
            }
            var user = new User
            {
                userId = SUPER_VARIABLE_INDEX_ID,  //索引id
                userName = userName,
                userPwd = EncryptHelper.SHA256(userpwd),
                regTime = DateTime.Now,
                role = int.Parse(role)
            };
            Dbop.Insert(userName, user.ToJsonString());
            if (info != null)
                ServerContext.logger.Info($"{info.username}添加了用户{userName}");
        }

        [ValidateAPI]
        [Secure(2)]
        public bool ValidateUserName(string isEdit, string oldUsername, string newUserName)
        {
            if (isEdit == "1" && oldUsername == newUserName)
            {
                return true;
            }

            return !Dbop.Exist(newUserName);

        }

        [API]
        [Secure(2), LoginInfo]
        public void RemoveUser(string userIndex, string userNames, LoginInfo info)
        {
            try
            {
                var arr = userIndex.Split(',');
                var userNameArr = userNames.Split(',');
                for (var i = arr.Length - 1; i > -1; i--)
                {
                    var userId = int.Parse(arr[i]);
                    Dbop.Delete(userId);//litedb不起作用
                    Dbop.DeleteHash(userNameArr[i]);
                    ServerContext.logger.Info($"{info.username}删除了用户{userNameArr[i]}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("删除用户出错：" + ex.Message);
            }
        }

        [API]
        [Secure(2), LoginInfo]
        public void ResetPwd(string userName, string userPwd, LoginInfo info)
        {

            if (!Dbop.Exist(userName))
            {
                throw new Exception($"error: user {userName} not exist.");
            }
            User user = Dbop.Get(userName)?.ToObject<User>();
            user.userPwd = EncryptHelper.SHA256(userPwd);
            Dbop.Update(userName, user.ToJsonString());
            ServerContext.logger.Info($"{info.username}重置了用户{userName}的密码");
        }
        #endregion

        #region 任务
        [API]
        [Secure]
        public IEnumerable<string> GetTasks()
        {
            return ServerContext.Tasks.Select(p => new
            {
                id = p.Id.ToString(),
                aid = p.AssemblyId.ToString(),
                name = p.Name,
                typeName = p.TypeName,
                status = p.Status.GetDescription(),
                nextProcessTime = p.NextProcessTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "无",
                timerType = p.TaskTimerType.GetDescription(),
                interval = p.Interval,
                runOnStart = p.RunOnStart ? "是" : "否",
                startTime = p.StartTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "无"
            }.ToJsonString());
        }

        [API]
        [Secure(1)]
        public void StartAllTasks()
        {
            ServerContext.StartAllTask();
        }

        [API]
        [Secure(1)]
        public void StopAllTasks()
        {
            ServerContext.StopAllTask();
        }

        [API]
        [Secure(1)]
        public void StartTask(string id)
        {
            ServerContext.StartTask(Guid.Parse(id));
        }

        [API]
        [Secure(1)]
        public void StopTask(string id)
        {
            ServerContext.StopTask(Guid.Parse(id));
        }

        [API]
        [Secure(1)]
        public void RunTask(string id)
        {
            ServerContext.RunImmediatelyTask(Guid.Parse(id));
        }

        [API]
        [Secure(1)]
        public TaskAgent GetTask(string id)
        {
            var gid = Guid.Parse(id);
            foreach (var t in ServerContext.Tasks)
            {
                if (t.Id == gid)
                {
                    if (t.Status == TaskStatus.Stop)
                        return t;
                    throw new TaskNotStopException();
                }
            }
            throw new TaskNotExistException();
        }

        [API]
        [Secure(1)]
        public void EditTaskRunParam(string id, string timerType, string interval, string startTime, string runOnStart)
        {
            DateTime? time = null;
            if (!string.IsNullOrEmpty(startTime) && DateTime.TryParse(startTime, out DateTime t))
            {
                time = t;
            }
            ServerContext.EditTaskRunParam(Guid.Parse(id), (TimerType)int.Parse(timerType), int.Parse(interval), time, bool.Parse(runOnStart));
        }

        [API]
        [Secure(1)]
        public object GetTaskConfig(string id)
        {
            var gid = Guid.Parse(id);
            foreach (var t in ServerContext.Tasks)
            {
                if (t.Id == gid)
                {
                    if (t.Status == TaskStatus.Stop)
                        return t.configuration.Values;
                    throw new TaskNotStopException();
                }
            }
            throw new TaskNotExistException();
        }

        [API]
        [Secure(1)]
        public void EditTaskConfig(string id, string configs)
        {
            var kv = JsonConvert.DeserializeObject<KeyValuePair<string, string>[]>(configs);
            ServerContext.EditTaskConfig(Guid.Parse(id), kv);
        }
        #endregion

        #region 程序集
        [API]
        [Secure(2)]
        public IEnumerable<string> GetAssemblies()
        {

            var res = new List<string>();
            Dictionary<string, List<string>> dic = new Dictionary<string, List<string>>();
            foreach (var task in ServerContext.Tasks)
            {
                var aid = task.AssemblyId.ToString();
                if (!dic.ContainsKey(aid))
                {
                    dic.Add(aid, new List<string>());
                }
                dic[aid].Add(task.TypeName);
            }
            foreach (var aid in dic.Keys)
            {
                var dir = new DirectoryInfo(Path.Join(ServerContext.AssemblyPath, aid));
                res.Add(new
                {
                    id = aid,
                    create = dir.CreationTime,
                    tasks = dic[aid],
                    files = dir.GetFiles().Select(p => p.Name).ToArray()
                }.ToJsonString());
            }
            return res;
        }

        [API]
        [Secure(2), LoginInfo]
        public void DelAssembly(string id, LoginInfo info)
        {
            var aid = Guid.Parse(id);
            if (ServerContext.Tasks.Count(t => t.AssemblyId == aid && t.Status != TaskStatus.Stop) > 0)
                throw new TaskNotStopException("该程序集中有任务未停止，请先停止任务！");
            ServerContext.DeleteAssembly(aid);
            ServerContext.logger.Info($"{info.username}删除了程序集{aid}");
        }


        [FileUpload]
        [Secure(2), LoginInfo]
        public void UploadAssembly(FileInfo fileInfo, LoginInfo info)
        {
            var assemblyId = Guid.NewGuid();
            var rootDir = Path.Join(ServerContext.AssemblyPath, assemblyId.ToString());
            Directory.CreateDirectory(rootDir);
            try
            {
                UnZip(fileInfo.FullName, rootDir, null);
                System.Threading.Thread.Sleep(500);
                ServerContext.LoadAssembly(assemblyId);
                ServerContext.logger.Info($"{info.username}添加了程序集{assemblyId.ToString()}");
            }
            catch
            {
                Directory.Delete(rootDir, true);
                throw;
            }
            finally
            {
                File.Delete(fileInfo.FullName);
            }
        }

        /// <summary>   
        /// 解压功能(解压压缩文件到指定目录)   
        /// </summary>   
        /// <param name="fileToUnZip">待解压的文件</param>   
        /// <param name="zipedFolder">指定解压目标目录</param>   
        /// <param name="password">密码</param>   
        /// <returns>解压结果</returns>   
        private bool UnZip(string fileToUnZip, string zipedFolder, string password)
        {
            bool result = true;
            FileStream fs = null;
            ZipInputStream zipStream = null;
            ZipEntry ent = null;
            string fileName;

            if (!File.Exists(fileToUnZip))
                return false;

            if (!Directory.Exists(zipedFolder))
                Directory.CreateDirectory(zipedFolder);

            try
            {
                zipStream = new ZipInputStream(File.OpenRead(fileToUnZip));
                if (!string.IsNullOrEmpty(password)) zipStream.Password = password;
                while ((ent = zipStream.GetNextEntry()) != null)
                {
                    if (!string.IsNullOrEmpty(ent.Name))
                    {
                        fileName = Path.Combine(zipedFolder, ent.Name);
                        fileName = fileName.Replace('/', '\\');//change by Mr.HopeGi   

                        if (fileName.EndsWith("\\"))
                        {
                            Directory.CreateDirectory(fileName);
                            continue;
                        }

                        fs = File.Create(fileName);
                        int size = 2048;
                        byte[] data = new byte[size];
                        while (true)
                        {
                            size = zipStream.Read(data, 0, data.Length);
                            if (size > 0)
                                fs.Write(data, 0, data.Length);
                            else
                                break;
                        }
                    }
                }
            }
            catch
            {
                result = false;
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                    fs.Dispose();
                }
                if (zipStream != null)
                {
                    zipStream.Close();
                    zipStream.Dispose();
                }
                if (ent != null)
                {
                    ent = null;
                }
                GC.Collect();
                GC.Collect(1);
            }
            return result;
        }
        #endregion

        #region 日志
        private IEnumerable<DirectoryInfo> GetLogDirectories()
        {
            var res = new List<DirectoryInfo>();
            res.Add(new DirectoryInfo(Path.Join(baseLogFilePath, MAIN_MODULE)));
            res.AddRange(ServerContext.Tasks.Select(p => new DirectoryInfo(Path.Join(baseLogFilePath, p.TypeName))));
            return res.Where(p => p.Exists);
        }

        [Secure]
        [API]
        public IEnumerable<string> GetLogNames()
        {
            return GetLogDirectories().Select(p => p.Name);
        }


        [Secure]
        [API]
        public IEnumerable<string> GetLogFiles(string number, string error)
        {
            int n = int.Parse(number);
            bool onlyError = bool.Parse(error);
            var logLevels = new List<Tuple<string, int>>() { new Tuple<string, int>("error", 1) };
            if (!onlyError)
                logLevels.Insert(0, new Tuple<string, int>("info", 0));
            List<string> res = new List<string>();
            var dirs = GetLogDirectories();
            foreach (var dir in dirs)
            {
                var logs = new List<LogInfo>();
                foreach (var level in logLevels)
                {
                    var levelDir = new DirectoryInfo(Path.Join(dir.FullName, level.Item1));
                    if (levelDir.Exists)
                    {
                        logs.AddRange(levelDir.GetFiles().Select(p => new LogInfo { name = p.Name, level = level.Item2 }).ToArray());
                    }
                }
                res.Add(new { name = dir.Name, logs = logs.OrderByDescending(p => p.name).Take(n) }.ToJsonString());
            }
            return res;
        }

        [Secure]
        [FormAPI]
        public string GetLogInfo(string log)
        {
            log = log.Replace('$', '/');
            var file = new FileInfo(Path.Join(baseLogFilePath, log));
            if (file.Exists)
            {
                using (var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.ReadWrite))
                {
                    var sr = new StreamReader(fs, Encoding.UTF8);
                    return string.Join("<br>", sr.Tail(1000));
                }
            }
            throw new FileNotFoundException("日志不存在");
        }

        [Secure(2)]
        [API]
        public void DeleteLog(string log)
        {
            log = log.Replace('$', '/');
            var file = new FileInfo(Path.Join(baseLogFilePath, log));
            if (file.Exists)
            {
                File.Delete(file.FullName);
                return;
            }
            throw new FileNotFoundException("日志不存在");
        }

        [Secure]
        [API]
        public string GetLogInfoRealtime(string taskname, string lastLines)
        {
            int lastLinesInt = int.Parse(lastLines);
            FileInfo[] files = new DirectoryInfo(Path.Join(baseLogFilePath, taskname, "info")).GetFiles();
            DateTime recentWrite = DateTime.MinValue;
            FileInfo recentFile = null;

            foreach (FileInfo file in files)
            {
                if (file.LastWriteTime > recentWrite)
                {
                    recentWrite = file.LastWriteTime;
                    recentFile = file;
                }
            }
            if (recentFile == null)
                return "";
            //文件会被独占
            using (var fs = new FileStream(recentFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.ReadWrite))
            {
                var sr = new StreamReader(fs);
                return string.Join("\r\n", sr.Tail(lastLinesInt));
            }
        }

        class LogInfo
        {
            public string name { get; set; }
            public int level { get; set; }
        }
        #endregion

        #region 帮助
        /// <summary>
        /// 返回一个未关闭的stream
        /// </summary>
        /// <param name="filekey"></param>
        /// <returns></returns>
        [Secure]
        [FileAPI]
        public FileDTO DownloadFile(string dllName)
        {
            FileInfo f = new FileInfo(dllName);
            if (f.Exists)
                return new FileDTO()
                {
                    FileName = f.Name,
                    FileStream = new FileStream(f.FullName, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.ReadWrite)
                };
            throw new FileNotFoundException();
        }
        #endregion
    }
}
