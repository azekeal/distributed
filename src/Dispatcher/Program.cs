using Common;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dispatcher
{
    public class StdInTaskProvider : ITaskProvider
    {
        private Queue<string> work = new Queue<string>();

        public event Action TasksAdded;

        public int TaskCount => 1;

        public object Config => "This is the opaque job config object";

        public bool CompleteTask(TaskItem task, TaskResult result)
        {
            Console.WriteLine($"Task completed {task.Identifier} ({task.Data})");
            return true;
        }

        public bool TryGetTask(out TaskItem task)
        {
            lock (work)
            {
                if (work.Count > 0)
                {
                    var line = work.Dequeue();
                    task = new TaskItem($"task_{Guid.NewGuid()}", line);
                    return true;
                }

                task = new TaskItem();
                return false;
            }
        }

        public void AddWork(string line)
        {
            lock (work)
            {
                Console.WriteLine("Work Added: " + line);
                work.Enqueue(line);
                TasksAdded?.Invoke();
            }
        }
    }


    public class Program
    {
        static void Main(string[] args)
        {
            // TODO: parse args
            var config = new Config();
            var taskProvider = new StdInTaskProvider();


            using (var dispatcher = new Dispatcher(config))
            {
                var shutdown = new CancellationTokenSource();

                Task.Run(() =>
                {
                    dispatcher.AddJob(taskProvider);

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
                            taskProvider.AddWork(line);
                        }
                    }
                });

                Task.Delay(-1, shutdown.Token).ContinueWith(t => { }).Wait();
            }
        }
    }
}
