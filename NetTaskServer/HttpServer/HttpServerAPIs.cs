using NetTaskManager;
using NetTaskServer.Common;
using NetTaskServer.DB;
using NetTaskServer.DB.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            string output = $"{username}|{DateTime.Now.ToString("yyyy-MM-dd")}";
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
        #endregion
    }
}
