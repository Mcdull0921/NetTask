using System;
using System.Collections.Generic;
using System.Text;

namespace NetTaskServer.DB.Model
{
    public class User
    {
        public string userId;
        public string userPwd;
        public string userName;
        public string email;
        public bool receiveEmail;
        public DateTime regTime;
        public int role;  //0普通用户 1管理员 2超级管理员
    }
}
