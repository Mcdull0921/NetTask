using NetTaskInterface;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetTaskManager
{
    public class TaskAgent
    {
        volatile TaskStatus taskStatus;
        readonly ITask task;
        readonly Guid id;
        readonly Guid assemblyId;
        TaskRunParam taskConfig;
        readonly object lockObject = new object();
        internal TaskAgent(ITask task, Guid assemblyId, TaskRunParam taskConfig)
        {
            this.task = task;
            taskStatus = TaskStatus.Stop;
            id = Guid.NewGuid();
            this.assemblyId = assemblyId;
            this.taskConfig = taskConfig;
        }

        internal void Start(TaskManager taskManager)
        {
            if (taskStatus != TaskStatus.Stop)
                return;
            if (taskConfig.runOnStart)
            {
                taskStatus = TaskStatus.Waitting;
                taskManager.AddProcessQueue(this, DateTime.Now);
            }
            else if (taskConfig.startTime.HasValue)
            {
                NextProcessTime = GetNextProcessTime(taskConfig.startTime.Value);
                if (NextProcessTime.HasValue)
                {
                    taskStatus = TaskStatus.Waitting;
                    taskManager.AddProcessQueue(this, NextProcessTime.Value);
                }
            }
        }

        internal void RunImmediately(TaskManager taskManager)
        {
            if (taskStatus != TaskStatus.Stop)
                return;
            taskStatus = TaskStatus.Waitting;
            taskManager.AddProcessQueue(this, DateTime.Now);
        }

        internal void Process()
        {
            try
            {
                if (taskStatus != TaskStatus.Waitting)
                    return;
                lock (lockObject)
                {
                    taskStatus = TaskStatus.Running;
                    task.process();
                    if (taskStatus == TaskStatus.WaittingForStop)
                    {
                        taskStatus = TaskStatus.Stop;
                        NextProcessTime = null;
                        return;
                    }
                    NextProcessTime = GetNextProcessTime(taskConfig.startTime ?? DateTime.Now);
                    taskStatus = NextProcessTime.HasValue ? TaskStatus.Waitting : TaskStatus.Stop;
                    task.logger.Info(string.Format("任务【{0}】执行完毕，状态【{1}】 下次执行时间：{2}", task.name, taskStatus.GetDescription(), NextProcessTime.HasValue ? NextProcessTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "无"));
                }
            }
            catch (Exception ex)
            {
                taskStatus = TaskStatus.Stop;
                NextProcessTime = null;
                task.logger.Error(ex, string.Format("TaskAgent.Process异常，任务【{0}】", task.name));
            }
        }

        internal void Stop()
        {
            if (taskStatus == TaskStatus.Running)
                taskStatus = TaskStatus.WaittingForStop;
            else if (taskStatus == TaskStatus.Waitting)
            {
                taskStatus = TaskStatus.Stop;
                NextProcessTime = null;
            }

        }

        private DateTime? GetNextProcessTime(DateTime lastTime)
        {
            switch (taskConfig.timerType)
            {
                case TimerType.Second:
                    return GetSecondTime(lastTime);
                case TimerType.Minute:
                    return GetMinuteTime(lastTime);
                case TimerType.Hour:
                    return GetHourTime(lastTime);
                case TimerType.Day:
                    return GetDayTime(lastTime);
                case TimerType.Month:
                    return GetMonthTime(lastTime);
                default:
                    if (lastTime > DateTime.Now)
                        return lastTime;
                    return null;
            }
        }

        private DateTime GetSecondTime(DateTime time)
        {
            if (time <= DateTime.Now)
            {
                var timeSpan = DateTime.Now - time;
                var add = (long)timeSpan.TotalSeconds / taskConfig.interval + 1;
                return time.AddSeconds(add * taskConfig.interval);
            }
            return time;
        }

        private DateTime GetMinuteTime(DateTime time)
        {
            if (time <= DateTime.Now)
            {
                var timeSpan = DateTime.Now - time;
                var add = (long)timeSpan.TotalMinutes / taskConfig.interval + 1;
                return time.AddMinutes(add * taskConfig.interval);
            }
            return time;
        }

        private DateTime GetHourTime(DateTime time)
        {
            if (time <= DateTime.Now)
            {
                var timeSpan = DateTime.Now - time;
                var add = (long)timeSpan.TotalHours / taskConfig.interval + 1;
                return time.AddHours(add * taskConfig.interval);
            }
            return time;
        }

        private DateTime GetDayTime(DateTime time)
        {
            if (time <= DateTime.Now)
            {
                var timeSpan = DateTime.Now - time;
                var add = (long)timeSpan.TotalDays / taskConfig.interval + 1;
                return time.AddDays(add * taskConfig.interval);
            }
            return time;
        }

        private DateTime GetMonthTime(DateTime time)
        {
            while (time <= DateTime.Now)
            {
                time = time.AddMonths(taskConfig.interval);
            }
            return time;
        }

        #region 对外属性
        public DateTime? NextProcessTime
        {
            get; private set;
        }

        public Guid Id
        {
            get { return id; }
        }

        public Guid AssemblyId
        {
            get { return assemblyId; }
        }

        public TaskStatus Status
        {
            get { return taskStatus; }
        }

        public string Name
        {
            get
            {
                return task.name;
            }
        }

        public string TypeName
        {
            get
            {
                return taskConfig.taskTypeName;
            }
        }
        public int Interval
        {
            get
            {
                return taskConfig.interval;
            }
        }
        public bool RunOnStart
        {
            get
            {
                return taskConfig.runOnStart;
            }
        }
        public TimerType TaskTimerType
        {
            get
            {
                return taskConfig.timerType;
            }
        }
        public DateTime? StartTime
        {
            get
            {
                return taskConfig.startTime;
            }
        }

        internal Configuration configuration
        {
            get
            {
                return task.configuration;
            }
            set
            {
                task.configuration = value;
            }
        }

        internal TaskRunParam runParam
        {
            get
            {
                return taskConfig;
            }
            set
            {
                taskConfig = value;
            }
        }
        #endregion
    }
}
