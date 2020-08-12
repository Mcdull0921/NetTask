using NetTaskManager;
using System;
using System.Text;

namespace NetTaskServer.Common
{
    class EmailHelper
    {
        string SmtpServerIp;
        int SmtpServerPort;
        string Account;
        string Passowrd;

        public EmailHelper(string smtpServerIp, int smtpServerPort, string account, string password)
        {
            this.SmtpServerIp = smtpServerIp;
            this.SmtpServerPort = smtpServerPort;
            this.Account = account;
            this.Passowrd = password;
        }
        public void SendEmail(string sendToAddress, string title, string content, Logger logger)
        {
            try
            {
                System.Net.Mail.SmtpClient client;                                           //邮件客户端
                client = new System.Net.Mail.SmtpClient(SmtpServerIp, SmtpServerPort);       //实例化对象，参数为smtp服务器
                client.Timeout = 60000;                                                      //邮件发送延迟一分钟
                client.EnableSsl = true;
                client.UseDefaultCredentials = false;
                client.Credentials = new System.Net.NetworkCredential(Account, Passowrd);    //登录名与密码
                client.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
                System.Net.Mail.MailMessage message = new System.Net.Mail.MailMessage();

                message.SubjectEncoding = Encoding.UTF8;
                message.BodyEncoding = Encoding.UTF8;
                message.From = new System.Net.Mail.MailAddress(Account, "NetTask", Encoding.UTF8);  // 【发件人地址,发件人称呼[发件人姓名]】--发件人地址需要与SMTP登录名保持一致
                message.To.Add(new System.Net.Mail.MailAddress(sendToAddress, "", Encoding.UTF8));
                message.IsBodyHtml = true;
                message.Subject = title.Replace("\r", "").Replace("\n", "").Trim();
                message.Body = content;

                client.Send(message);
            }
            catch (Exception ex)
            {
                logger.Error($"邮件发送{sendToAddress}失败", ex);
            }
        }
    }
}
