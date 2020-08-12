using System;
using System.Collections.Generic;
using System.Text;

namespace NetTaskInterface
{
    public class Logger
    {
        public event Action<string> onInfo;
        public event Action<string, Exception, bool> onError;
        public event Action<string, string> onMail;

        /// <summary>
        /// 输出常规日志
        /// </summary>
        /// <param name="message">内容</param>
        public void Info(string message)
        {
            onInfo?.Invoke(message);
        }

        /// <summary>
        /// 输出错误日志
        /// </summary>
        /// <param name="ex">异常</param>
        /// <param name="message">内容</param>
        /// <param name="sendEmail">是否邮件通知，需在后台管理中开启邮件通知，并配置smtp相关信息</param>
        public void Error(Exception ex, string message = "", bool sendEmail = false)
        {
            onError?.Invoke(message, ex, sendEmail);
        }

        /// <summary>
        /// 直接发送邮件
        /// </summary>
        /// <param name="receiver">接收人，任意邮箱号</param>
        /// <param name="message">邮件内容</param>
        public void Mail(string receiver, string message)
        {
            onMail?.Invoke(receiver, message);
        }

        /// <summary>
        /// 发送邮件，会向所有后端配置接收邮件的账号发送
        /// </summary>
        /// <param name="message">邮件内容</param>
        public void Mail(string message)
        {
            onMail?.Invoke(null, message);
        }
    }
}
