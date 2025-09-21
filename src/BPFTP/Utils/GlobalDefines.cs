using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPFTP.Utils
{
    public static class GlobalDefines
    {
        public readonly static Func<Task> DummyTask = () => Task.CompletedTask;
    }
}
