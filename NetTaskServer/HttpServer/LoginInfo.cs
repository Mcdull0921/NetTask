using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace NetTaskServer.HttpServer
{
    class LoginInfo
    {
        public string username { get; set; }
        public int role { get; set; }
        public IPAddress ip { get; set; }
    }
}
