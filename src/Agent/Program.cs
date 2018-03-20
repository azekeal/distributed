using Distributed;
using Distributed.Core;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mock
{
    public class MockTaskExecutor : TaskExecutor
    {
        public override Task<InitializationResult> Initialize(object config)
        {
            Console.WriteLine("Initialize: " + config);
            return Task.FromResult(new InitializationResult
            { 
                success = true,
                capacity = 3,
                errorMessage = null,
            });
        }

        public override Task<TaskResult[]> StartTasks(TaskItem[] tasks)
        {
            foreach (var task in tasks)
            {
                Console.WriteLine($"StartTask: {task.Identifier}, data:{task.Data}");
            }

            var results = tasks.Select(t => new TaskResult()
            {
                success = true,
                errorMessage = null,
                data = t.Data
            }).ToArray();

            Task.Delay(5000).ContinueWith(t =>
            {
                foreach (var task in tasks)
                {
                    Console.WriteLine($"CompleteTask: {task.Identifier}, data:{task.Data}");
                    CompleteTask(task, new TaskResult()
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
            var config = new AgentConfig();

            if (args.Length > 0)
            {
                config.CoordinatorAddress = args[0];
                Console.WriteLine($"CoordinatorAddress: http://{config.CoordinatorAddress}/");
            }

            if (args.Length > 1)
            {
                config.AgentPort = int.Parse(args[1]);
                config.WebPort = config.AgentPort + 1;
                Console.WriteLine($"AgentHost: http://localhost:{config.AgentPort}/");
            }

            using (var agent = new Agent(config, new MockTaskExecutor()))
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
                    }
                });

                Task.Delay(-1, shutdown.Token).ContinueWith(t => { }).Wait();
            }
        }
    }
}
