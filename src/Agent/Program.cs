using Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Agent
{
    public class Program
    {
        static void Main(string[] args)
        {
            using (var agent = new Agent())
            {
                var shutdown = new CancellationTokenSource();

                Task.Run(() =>
                {
                    while (true)
                    {
                        var line = Console.ReadLine();
                        if (line.Length == 0)
                        {
                            shutdown.Cancel();
                            break;
                        }
                        else
                        {
                            //coordinator.SendMessage("hello");
                        }
                    }
                });

                Task.Delay(-1, shutdown.Token).ContinueWith(t => { }).Wait();
            }
        }
    }
}
