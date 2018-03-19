﻿using Distributed.Core;
using System;
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

        private object lockObj = new object();
        private Queue<TaskItem> returnedTasks = new Queue<TaskItem>();
        private ITaskProvider taskProvider;

        public Job(Distributed.Dispatcher dispatcher, ITaskProvider taskProvider, int priority)
        {
            this.Dispatcher = dispatcher;
            this.Name = $"job_{Guid.NewGuid()}";
            this.Priority = priority;
            this.TaskCount = taskProvider.TaskCount;
            this.taskProvider = taskProvider;
            this.taskProvider.TasksAdded += NotifyTasksAvailable;
        }

        public void Start()
        {
            // Dispatcher will assign agents to the dispatcher based on the job priority & size
            Dispatcher.Coordinator.SetActiveJob(Name, Priority, TaskCount);
        }

        public List<TaskItem> GetTasks(int capacity, Action notifyOnTasksAvailable)
        {
            lock (lockObj)
            {
                var list = new List<TaskItem>();

                while (capacity > 0 && returnedTasks.Count > 0)
                {
                    list.Add(returnedTasks.Dequeue());
                    capacity--;
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
            if (taskProvider.CompleteTask(task, result))
            {
                NotifyTasksAvailable();
            }
            else if (taskProvider.TaskCount == 0)
            {
                Completed?.Invoke();
            }
        }

        private void NotifyTasksAvailable()
        {
            lock (lockObj)
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
            lock (lockObj)
            {
                foreach (var task in tasks)
                {
                    returnedTasks.Enqueue(task);
                }

                if (returnedTasks.Count > 0)
                {
                    NotifyTasksAvailable();
                }
            }
        }

        public void Dispose()
        {
            taskProvider.TasksAdded -= NotifyTasksAvailable;
        }
    }
}