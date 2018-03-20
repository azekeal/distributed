﻿using Distributed.Core;
using Distributed.Internal.Client;
using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Distributed.Internal.Dispatcher
{
    public enum TaskState
    {
        Pending,
        Active,
        Completed,
        Failed,
        Cancelled
    }

    public class Agent : Endpoint
    {

        public string Identifier => Info.name;
        public int Capacity { get; private set; }

        public event Action<Agent, TaskState, IEnumerable<TaskItem>> TaskStateChanged;
        public event Action<Agent, int> CapacityChanged;
        public event Action<Agent> Disposed;

        private Distributed.Dispatcher dispatcher;
        private AgentConnection connection;
        private Dictionary<string, TaskItem> pendingTasks;
        private Dictionary<string, TaskItem> activeTasks;
        private Job activeJob;
        private bool initialized;
        private bool disposed;
        private object lockObj = new object();


        public IEnumerable<TaskItem> PendingTasks
        {
            get
            {
                lock (lockObj)
                {
                    return pendingTasks.Values.ToArray();
                }
            }
        }

        public IEnumerable<TaskItem> ActiveTasks
        {
            get
            {
                lock (lockObj)
                {
                    return activeTasks.Values.ToArray();
                }
            }
        }


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
                Console.WriteLine($"Disposing {Identifier}");

                Disposed?.Invoke(this);

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

                TaskStateChanged = null;
            }
        }
        
        private void OnActiveJobChanged(Job obj) => RequestTasks();

        private void OnTaskCompleted(TaskItem task, TaskResult result)
        {
            lock (lockObj)
            {
                if (disposed)
                {
                    Console.WriteLine($"WARNING: Agent {Identifier} returning tasks ({task}) when disposed");
                    return;
                }

                if (!initialized)
                {
                    Console.WriteLine($"WARNING: Agent {Identifier} returning tasks ({task}) when not initialized");
                    return;
                }

                Console.WriteLine($"Agent {Identifier} Task Completed {task}");

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
            }

            // exit agent lock before calling job to prevent potential deadlocks
            activeJob.CompleteTask(task, result);

            RequestTasks();

            lock (lockObj)
            {
                if (TaskStateChanged != null)
                {
                    TaskStateChanged?.Invoke(this, TaskState.Completed, new TaskItem[] { task });
                }
            }
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
                    Capacity = initResult.capacity;
                    CapacityChanged?.Invoke(this, Capacity);

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
                if (queued < Capacity)
                {
                    StartTasks(Capacity - queued);
                }
            }
        }

        /// <summary>
        /// Gets up to count new tasks and adds them to the pending set
        /// </summary>
        private List<TaskItem> GetNewTasks(int count)
        {
            // get tasks outside lock to avoid potential deadlock (job has its own lock)
            var tasks = activeJob.GetTasks(count, notifyOnTasksAvailable: RequestTasks);

            lock (lockObj)
            {
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
                TaskStateChanged?.Invoke(this, TaskState.Pending, tasks);

                try
                {
                    var results = await connection.StartTasks(tasks);
                    ProcessStartTaskResults(tasks, results);
                }
                catch(Exception e)
                {
                    Console.WriteLine($"Failed to start tasks ({e.Message})");
                }
            }
        }

        private void ProcessStartTaskResults(List<TaskItem> tasks, TaskResult[] results)
        {
            Debug.Assert(results.Length == tasks.Count);
            Console.WriteLine("ProcessStartTaskResults");

            var successful = new List<TaskItem>();
            var failed = new List<TaskItem>();

            for (int i = 0; i < results.Length; ++i)
            {
                var task = tasks[i];
                var result = results[i];

                if (result.success)
                {
                    Console.WriteLine($"Task started {task.Identifier}");
                    successful.Add(task);

                    lock (lockObj)
                    {
                        activeTasks.Add(task.Identifier, task);
                    }
                }
                else
                {
                    Console.WriteLine($"Failed to start task {task.Identifier}");
                    Console.WriteLine(result.errorMessage);
                    failed.Add(task);

                    // we wan't all calls to job to be outside the lock to prevent potential deadlocks
                    activeJob.CompleteTask(task, result);

                }

                lock (lockObj)
                {
                    pendingTasks.Remove(task.Identifier);
                }
            }

            lock (lockObj)
            {
                if (TaskStateChanged != null)
                {
                    if (successful.Count > 0)
                    {
                        TaskStateChanged.Invoke(this, TaskState.Active, successful);
                    }

                    if (failed.Count > 0)
                    {
                        TaskStateChanged.Invoke(this, TaskState.Failed, failed);
                    }
                }
            }
        }
    }
}

