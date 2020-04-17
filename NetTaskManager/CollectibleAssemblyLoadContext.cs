using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace NetTaskManager
{
    class CollectibleAssemblyLoadContext : AssemblyLoadContext
    {
        public Guid id { get; private set; }
        public CollectibleAssemblyLoadContext(Guid id) : base(isCollectible: true)
        {
            this.id = id;
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            return null;
        }
    }
}
