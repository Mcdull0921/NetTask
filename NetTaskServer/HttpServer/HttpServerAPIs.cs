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

namespace NetTaskServer.HttpServer
{
    class HttpServerAPIs
    {
        public const string SUPER_VARIABLE_INDEX_ID = "$index_id$";
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
        public string Login(string username, string userpwd)
        {
            //1.校验
            dynamic user = Dbop.Get(username)?.ToDynamic();
            if (user == null)
            {
                return "Error: User not exist.Please <a href='javascript:history.go(-1)'>go backward</a>.";
            }


            if (user.userPwd != EncryptHelper.SHA256(userpwd))
            {
                return "Error: Wrong password.Please <a href='javascript:history.go(-1)'>go backward</a>.";
            }

            //2.给token
            string output = $"{username}|{DateTime.Now.ToString("yyyy-MM-dd")}|{user["role"].Value}";
            string token = EncryptHelper.AES_Encrypt(output);
            return string.Format(@"
<html>
<head><script>
document.cookie='NSPTK={0}; path=/;';
document.write('Redirecting...');
window.location.href='main.html';
</script>
</head>
</html>
            ", token);
        }
        #endregion

        #region 用户
        [API]
        [Secure]
        public List<string> GetUsers()
        {
            List<string> userStrList = Dbop.Select(0, 999);
            return userStrList;
        }

        [API]
        [Secure]
        public void AddUserV2(string userName, string userpwd, string role)
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
        }

        [ValidateAPI]
        [Secure]
        public bool ValidateUserName(string isEdit, string oldUsername, string newUserName)
        {
            if (isEdit == "1" && oldUsername == newUserName)
            {
                return true;
            }

            return !Dbop.Exist(newUserName);

        }

        [API]
        [Secure]
        public void RemoveUser(string userIndex, string userNames)
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
                }
            }
            catch (Exception ex)
            {
                throw new Exception("删除用户出错：" + ex.Message);
            }
        }

        [API]
        [Secure]
        public void ResetPwd(string userName, string userPwd)
        {

            if (!Dbop.Exist(userName))
            {
                throw new Exception($"error: user {userName} not exist.");
            }
            User user = Dbop.Get(userName)?.ToObject<User>();
            user.userPwd = EncryptHelper.SHA256(userPwd);
            Dbop.Update(userName, user.ToJsonString());
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
        [Secure]
        public void StartAllTasks()
        {
            ServerContext.StartAllTask();
        }

        [API]
        [Secure]
        public void StopAllTasks()
        {
            ServerContext.StopAllTask();
        }

        [API]
        [Secure]
        public void StartTask(string id)
        {
            ServerContext.StartTask(Guid.Parse(id));
        }

        [API]
        [Secure]
        public void StopTask(string id)
        {
            ServerContext.StopTask(Guid.Parse(id));
        }

        [API]
        [Secure]
        public void RunTask(string id)
        {
            ServerContext.RunImmediatelyTask(Guid.Parse(id));
        }

        [API]
        [Secure]
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
        [Secure]
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
        [Secure]
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
        [Secure]
        public void EditTaskConfig(string id, string configs)
        {
            var kv = JsonConvert.DeserializeObject<KeyValuePair<string, string>[]>(configs);
            ServerContext.EditTaskConfig(Guid.Parse(id), kv);
        }
        #endregion

        #region 程序集
        [API]
        [Secure]
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
        [Secure]
        public void DelAssembly(string id)
        {
            var aid = Guid.Parse(id);
            if (ServerContext.Tasks.Count(t => t.AssemblyId == aid && t.Status != TaskStatus.Stop) > 0)
                throw new TaskNotStopException("该程序集中有任务未停止，请先停止任务！");
            ServerContext.DeleteAssembly(aid);
        }


        [FileUpload]
        [Secure]
        public void UploadAssembly(FileInfo fileInfo)
        {
            var assemblyId = Guid.NewGuid();
            var rootDir = Path.Join(ServerContext.AssemblyPath, assemblyId.ToString());
            Directory.CreateDirectory(rootDir);
            try
            {
                UnZip(fileInfo.FullName, rootDir, null);
                System.Threading.Thread.Sleep(500);
                ServerContext.LoadAssembly(assemblyId);
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
        [Secure]
        [API]
        public IEnumerable<string> GetLogFiles(string number)
        {
            int n = int.Parse(number);
            List<string> res = new List<string>();
            DirectoryInfo root = new DirectoryInfo(baseLogFilePath);
            var dirs = root.GetDirectories();
            foreach (var dir in dirs)
            {
                var logs = new List<LogInfo>();
                var logLevels = new Tuple<string, int>[] { new Tuple<string, int>("info", 0), new Tuple<string, int>("error", 1) };
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
        [API]
        public string[] GetLogFileInfo(string lastLines)
        {
            int lastLinesInt = int.Parse(lastLines);
            string baseLogPath = "./log";
            DirectoryInfo dir = new DirectoryInfo(baseLogPath);
            FileInfo[] files = dir.GetFiles("*.log*");
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

            //文件会被独占
            using (var fs = new FileStream(recentFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.ReadWrite))
            {
                var sr = new StreamReader(fs);
                return sr.Tail(lastLinesInt);
            }
        }

        class LogInfo
        {
            public string name { get; set; }
            public int level { get; set; }
        }
        #endregion
    }
}
