using System;
using System.IO;
using System.Threading;

namespace Distributed.Internal
{
    public static class Trace
    {
        private static ThreadLocal<int> indentation = new ThreadLocal<int>();

        public class Indent : IDisposable
        {
            public Indent()
            {
                indentation.Value++;
            }

            public void Dispose()
            {
                indentation.Value = Math.Max(0, indentation.Value - 1);
            }
        }

        public static IDisposable Log(
            string message = "", 
            [System.Runtime.CompilerServices.CallerFilePath] string file = "",
            [System.Runtime.CompilerServices.CallerMemberName] string member = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int line = 0
            )
        {
            Console.WriteLine($"{new string(' ', indentation.Value)}[{Path.GetFileNameWithoutExtension(file)}.{member}:{line}] {message}");
            return new Indent();
        }
    }
}
