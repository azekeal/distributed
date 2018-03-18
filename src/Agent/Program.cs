using Common;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Agent
{
    public class MockAgent : IAgent
    {
        public override Task<InitializationResult> Initialize(object config)
        {
            return Task.FromResult(new InitializationResult
            { 
                success = true,
                capacity = 1,
                errorMessage = null,
            });
        }

        public override Task<TaskResult[]> StartTasks(TaskItem[] tasks)
        {
            var results = tasks.Select(t => new TaskResult()
            {
                success = true,
                errorMessage = null,
                data = null
            }).ToArray();

            Task.Delay(1000).ContinueWith(t =>
            {
                foreach (var task in tasks)
                {
                    TaskCompleted(task, new TaskResult()
                    {
                        success = true,
                        errorMessage = null,
                        data = task.Data
                    });
                }
            });

            return Task.FromResult(results);
        }
    }

    public class Program
    {
        static void Main(string[] args)
        {
            using (var agent = new Agent(new MockAgent()))
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
