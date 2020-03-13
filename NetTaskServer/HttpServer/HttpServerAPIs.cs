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
                AddUserV2("admin", "admin", "1");
            }
        }

        #region 登录
        [API]
        [Secure]
        public void AddUserV2(string userName, string userpwd, string isAdmin)
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
                role = 2
            };
            Dbop.Insert(userName, user.ToJsonString());
        }

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
        #endregion
    }
}
