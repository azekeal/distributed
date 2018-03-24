using Distributed.Core;
using Distributed.Internal.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Distributed.Internal.Dispatcher
{

    public class Job : IDisposable
    {
        public delegate void TasksAvailableHandler();

        public Distributed.Dispatcher Dispatcher { get; private set; }
        public string Name { get; private set; }
        public int Priority { get; private set; }
        public int TaskCount { get; private set; }
        public object Config => taskProvider.Config;

        public event Action Completed;
        public event Action TasksAvailable;

        private ConcurrentQueue<TaskItem> returnedTasks = new ConcurrentQueue<TaskItem>();
        private ITaskProvider taskProvider;

        public Job(Distributed.Dispatcher dispatcher, ITaskProvider taskProvider, int priority)
        {
            using (Trace.Log())
            {
                this.Dispatcher = dispatcher;
                this.Name = $"job_{Guid.NewGuid()}";
                this.Priority = priority;
                this.TaskCount = taskProvider.TaskCount;
                this.taskProvider = taskProvider;
                this.taskProvider.TasksAdded += NotifyTasksAvailable;
            }
        }

        public void Start()
        {
            using (Trace.Log())
            {
                Dispatcher.UpdateJob(this);
            }
        }

        public List<TaskItem> GetTasks(int capacity, Action notifyOnTasksAvailable)
        {
            using (Trace.Log())
            {
                var list = new List<TaskItem>();

                while (capacity > 0 && returnedTasks.Count > 0)
                {
                    if (returnedTasks.TryDequeue(out var taskItem))
                    {
                        list.Add(taskItem);
                        capacity--;

                    }
                }

                while (capacity > 0 && taskProvider.TryGetTask(out var task))
                {
                    list.Add(task);
                    capacity--;
                }

                // don't have enough tasks to give the requestor
                if (capacity > 0)
                {
                    Console.WriteLine("Waiting for tasks: " + notifyOnTasksAvailable);
                    TasksAvailable -= notifyOnTasksAvailable;
                    TasksAvailable += notifyOnTasksAvailable;
                }

                return list;
            }
        }

        public void CompleteTask(TaskItem task, TaskResult result)
        {
            using (Trace.Log())
            {
                if (taskProvider.CompleteTask(task, result))
                {
                    NotifyTasksAvailable();
                }
                else if (taskProvider.TaskCount == 0)
                {
                    Completed?.Invoke();
                }

                Dispatcher.UpdateJob(this);
            }
        }

        private void NotifyTasksAvailable()
        {
            using (Trace.Log())
            {
                Console.WriteLine("NotifyTasksAvailable: " + TasksAvailable);

                // clear the invocation list
                var listToNotify = TasksAvailable;
                TasksAvailable = null;
                listToNotify?.Invoke();

                Console.WriteLine("Clearing waiting tasks");
            }
        }

        public void CancelTasks(IEnumerable<TaskItem> tasks)
        {
            using (Trace.Log())
            {
                foreach (var task in tasks)
                {
                    returnedTasks.Enqueue(task);
                }

                if (returnedTasks.Count > 0)
                {
                    NotifyTasksAvailable();
                }

                Dispatcher.UpdateJob(this);
            }
        }

        public void Dispose()
        {
            using (Trace.Log())
            {
                taskProvider.TasksAdded -= NotifyTasksAvailable;
            }
        }
    }
}
