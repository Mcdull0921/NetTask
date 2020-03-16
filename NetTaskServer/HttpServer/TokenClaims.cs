using System;
using System.Collections.Generic;
using System.Text;

namespace NetTaskServer.HttpServer
{
    public class TokenClaims
    {
        public string UserKey { get; set; }
        public DateTime LastTime { get; set; }
        public int Role { get; set; }
    }
}
