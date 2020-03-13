using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace NetTaskManager
{
    class CollectibleAssemblyLoadContext : AssemblyLoadContext
    {
        public CollectibleAssemblyLoadContext()
        { }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            return null;
        }
    }
}
