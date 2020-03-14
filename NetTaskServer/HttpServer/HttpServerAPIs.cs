using NetTaskManager;
using NetTaskServer.Common;
using NetTaskServer.DB;
using NetTaskServer.DB.Model;
using System;
using System.Collections.Generic;
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
    }
}
