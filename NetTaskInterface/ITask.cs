using System;

namespace NetTaskInterface
{
    public abstract class ITask
    {
        public abstract void process();
        public abstract string name { get; }

        public Configuration.Config configuration { get; set; }

        public Logger logger { get; private set; }

        public ITask()
        {
            configuration = new Configuration().GetConfig(GetType());
            logger = new Logger();
        }
    }
}
