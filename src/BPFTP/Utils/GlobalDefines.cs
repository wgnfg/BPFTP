using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPFTP.Utils
{
    public static class GlobalDefines
    {
        public readonly static Func<Task> DummyTask = () => Task.CompletedTask;

        public static double mbMultiply = 1d / (1024 * 1024);

        public static double msMultiply = 1 / (double)Stopwatch.Frequency;
        public static double ToMB(this long value) => value * mbMultiply;
        public static double ToMB(this ulong value) => value * mbMultiply;

        public static double ToSecond(this long value) => value * msMultiply;
    }
}
