using System;
using System.Collections.Generic;
using System.Text;

namespace NetTaskServer.DB.Model
{
    public class MailAccount
    {
        public string smtpServer;
        public int smtpPort;
        public string userName;
        public string password;
        public string content;
        public bool enable;

        public static MailAccount Default()
        {
            return new MailAccount()
            {
                enable = false
            };
        }
    }
}
