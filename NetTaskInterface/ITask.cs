using System;

namespace NetTaskInterface
{
    public abstract class ITask
    {
        public abstract void process();
        public abstract string name { get; }

        public Configuration configuration { get; set; }

        public NLog.Logger logger { get; private set; }

        public ITask()
        {
            configuration = new Configuration();
            logger = NLog.LogManager.GetLogger(GetType().FullName);
        }
    }
}
