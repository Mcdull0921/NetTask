using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace NetTaskServer.Data
{
    class LoginInfo
    {
        public string username { get; set; }
        public int role { get; set; }
        public IPAddress ip { get; set; }
    }
}
