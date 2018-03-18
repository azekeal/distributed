using Common;
using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Dispatcher
{
    public class Agent : Endpoint
    {
        public string Identifier { get; private set; }

        private Dispatcher dispatcher;
        private AgentConnection connection;
        private Dictionary<string, TaskItem> pendingTasks;
        private Dictionary<string, TaskItem> activeTasks;
        private int capacity;
        private Job activeJob;
        private bool initialized;
        private bool disposed;
        private object lockObj = new object();

        public Agent(Dispatcher dispatcher, EndpointConnectionInfo info) : base(info)
        {
            this.dispatcher = dispatcher;
            this.pendingTasks = new Dictionary<string, TaskItem>();
            this.activeTasks = new Dictionary<string, TaskItem>();

            this.dispatcher.ActiveJobChanged += OnActiveJobChanged;

            this.connection = new AgentConnection(dispatcher, info);
            this.connection.StateChanged += OnStateChanged;
            this.connection.SetAgentIdentifier += SetAgentIdentifier;
            this.connection.TaskCompleted += OnTaskCompleted;
            this.connection.Start();
        }

        private void OnActiveJobChanged(Job obj) => RequestTasks();

        private void OnTaskCompleted(TaskItem task, TaskResult result)
        {
            lock (lockObj)
            {
                if (result.success)
                {
                    Console.WriteLine($"Task completed successfully {task.Identifier}");
                }
                else
                {
                    Console.WriteLine($"Task failed {task.Identifier}");
                    Console.WriteLine(result.errorMessage);
                }

                activeJob.CompleteTask(task, result);

                RequestTasks();
            }
        }

        private void SetAgentIdentifier(string id)
        {
            Identifier = id;
            Console.WriteLine($"Connected to agent {Identifier}");

            // Agent accepts, start giving it work
            RequestTasks();
        }

        private void OnStateChanged(StateChange stateChange)
        {
            if (stateChange.NewState == ConnectionState.Connected)
            {
                //RequestTasks();
            }
        }

        private async void InitializeForJobAsync(Job job)
        {
            initialized = false;
            activeJob = job;

            if (!initialized)
            {
                var initResult = await connection.Initialize(job.Config);
                if (initResult.success)
                {
                    Console.WriteLine($"Agent initialized {Identifier}");

                    initialized = true;
                    capacity = initResult.capacity;
                    RequestTasks();
                }
                else
                {
                    Console.WriteLine($"Agent initialization error {Identifier}");
                    Console.WriteLine(initResult.errorMessage);

                    dispatcher.DiscardAgent(this);
                }
            }
        }

        private void RequestTasks()
        {
            lock (lockObj)
            {
                if (disposed || dispatcher == null)
                {
                    return;
                }

                if (activeJob != dispatcher.ActiveJob)
                {
                    // let the current job complete any tasks
                    if (activeTasks.Count > 0)
                    {
                        return;
                    }

                    // start new job
                    InitializeForJobAsync(dispatcher.ActiveJob);
                    return;
                }

                // can't give out tasks until we're initialized
                if (!initialized || disposed || activeJob == null)
                {
                    return;
                }

                if (activeTasks.Count < capacity)
                {
                    StartTasks(capacity - activeTasks.Count);
                }
            }
        }

        /// <summary>
        /// Gets up to count new tasks and adds them to the pending set
        /// </summary>
        private List<TaskItem> GetNewTasks(int count)
        {
            lock (lockObj)
            {
                var tasks = activeJob.GetTasks(count, notifyOnTasksAvailable: RequestTasks);
                tasks.ForEach(t => pendingTasks.Add(t.Identifier, t));
                return tasks;
            }
        }

        private async void StartTasks(int count)
        {
            var tasks = GetNewTasks(count);
            var results = await connection.StartTasks(tasks);
            Debug.Assert(results.Length == tasks.Count);

            lock (lockObj)
            {
                for (int i = 0; i < results.Length; ++i)
                {
                    var task = tasks[i];
                    var result = results[i];

                    if (result.success)
                    {
                        activeTasks.Add(task.Identifier, task);
                    }
                    else
                    {
                        Console.WriteLine($"Failed to start task {task.Identifier}");
                        Console.WriteLine(result.errorMessage);

                        activeJob.CompleteTask(task, result);
                    }

                    pendingTasks.Remove(task.Identifier);
                }
            }
        }

        public override void Dispose()
        {
            lock (lockObj)
            {
                disposed = true;

                activeJob?.CancelTasks(pendingTasks.Values);
                activeJob?.CancelTasks(activeTasks.Values);
                activeJob = null;

                activeTasks?.Clear();
                activeTasks = null;

                if (dispatcher != null)
                {
                    dispatcher.ActiveJobChanged -= OnActiveJobChanged;
                    dispatcher = null;
                }

                connection?.Dispose();
                connection = null;
            }
        }
    }
}
