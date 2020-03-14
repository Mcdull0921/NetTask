using System;
using System.Collections.Generic;
using System.Text;

namespace NetTaskManager
{
    public class TaskNotExistException : Exception
    {
        public TaskNotExistException() : this("指定的任务不存在")
        {

        }
        public TaskNotExistException(string msg) : base(msg)
        {

        }
    }

    public class TaskNotStopException : Exception
    {
        public TaskNotStopException() : this("任务尚未结束，请先停止任务")
        {

        }
        public TaskNotStopException(string msg) : base(msg)
        {

        }
    }
}
