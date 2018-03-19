using Distributed.Core;
using Distributed.Internal.Client;
using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Distributed.Internal.Dispatcher
{
    public class Agent : Endpoint
    {
        public string Identifier { get; private set; }

        public event Action<Agent, TaskItem[], TaskItem[]> TasksChanged;

        private Distributed.Dispatcher dispatcher;
        private AgentConnection connection;
        private Dictionary<string, TaskItem> pendingTasks;
        private Dictionary<string, TaskItem> activeTasks;
        private int capacity;
        private Job activeJob;
        private bool initialized;
        private bool disposed;
        private object lockObj = new object();

        public Agent(Distributed.Dispatcher dispatcher, EndpointConnectionInfo info) : base(info)
        {
            this.dispatcher = dispatcher;
            this.pendingTasks = new Dictionary<string, TaskItem>();
            this.activeTasks = new Dictionary<string, TaskItem>();

            this.dispatcher.ActiveJobChanged += OnActiveJobChanged;

            this.connection = new AgentConnection(dispatcher, info);
            this.connection.StateChanged += OnStateChanged;
            this.connection.SetAgentState += SetAgentState;
            this.connection.TaskCompleted += OnTaskCompleted;
            this.connection.Start();
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

                TasksChanged = null;
            }
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

                activeTasks.Remove(task.Identifier);
                activeJob.CompleteTask(task, result);

                RequestTasks();
            }

            NotifyTasksChanged();
        }

        private void OnStateChanged(StateChange stateChange)
        {
            if (stateChange.NewState == ConnectionState.Connected)
            {
                //RequestTasks();
            }
        }

        private void SetAgentState(string agentId, bool activate)
        {
            if (activate)
            {
                Identifier = agentId;
                Console.WriteLine($"Connected to agent {Identifier}");

                // Agent accepts, start giving it work
                RequestTasks();
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

                    dispatcher.DiscardAgent(Identifier);
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

                var queued = pendingTasks.Count + activeTasks.Count;
                if (queued < capacity)
                {
                    StartTasks(capacity - queued);
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
                if (tasks.Count > 0)
                {
                    foreach (var task in tasks)
                    {
                        Console.WriteLine("Add to pending: " + task);
                        pendingTasks.Add(task.Identifier, task);
                    }
                }

                return tasks;
            }
        }

        private async void StartTasks(int count)
        {
            var tasks = GetNewTasks(count);
            Console.WriteLine($"StartTasks: {tasks.Count}/{count}");

            if (tasks.Count > 0)
            {
                var results = await connection.StartTasks(tasks);
                ProcessStartTaskResults(tasks, results);

                NotifyTasksChanged();
            }
        }

        private void NotifyTasksChanged()
        {
            lock (lockObj)
            {
                if (TasksChanged != null)
                {
                    TasksChanged.Invoke(this, pendingTasks.Values.ToArray(), activeTasks.Values.ToArray());
                }
            }
        }

        private void ProcessStartTaskResults(List<TaskItem> tasks, TaskResult[] results)
        {
            Debug.Assert(results.Length == tasks.Count);
            Console.WriteLine("ProcessStartTaskResults");

            lock (lockObj)
            {
                for (int i = 0; i < results.Length; ++i)
                {
                    var task = tasks[i];
                    var result = results[i];

                    if (result.success)
                    {
                        Console.WriteLine($"Task started {task.Identifier}");
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
    }
}

