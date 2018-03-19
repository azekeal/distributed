﻿using Distributed.Internal;
using Distributed.Internal.Dispatcher;
using Distributed.Internal.Util;
using Distributed.Monitor;
using Microsoft.Owin.Hosting;
using System;
using System.Collections.Generic;

namespace Distributed
{
    public sealed class Dispatcher : IDisposable
    {
        public static Dispatcher Instance { get;private set; }

        public string Identifier { get; private set; }
        public string EndpointData { get; private set; }
        public CoordinatorConnection Coordinator { get; private set; }
        public Job ActiveJob { get; private set; }
        public DispatcherConfig Config { get; private set; }
        public DispatcherMonitor Monitor { get; private set; }

        public event Action<Job> ActiveJobChanged;

        internal AgentPool Agents { get; private set; }

        private SortedList<int, Job> jobQueue;
        private IDisposable host;

        public Dispatcher() : this(new DispatcherConfig()) { }

        public Dispatcher(DispatcherConfig config)
        {
            Instance = this;

            this.Identifier = $"{Constants.Names.Dispatcher}_{Guid.NewGuid()}";
            this.EndpointData = $"127.0.0.1:{config.WebPort}";
            this.Config = config;

            this.Agents = new AgentPool(this);
            this.jobQueue = new SortedList<int, Job>();

            this.Coordinator = new CoordinatorConnection($"http://{config.CoordinatorAddress}/signalr", Identifier, EndpointData, "DispatcherHub");
            this.Coordinator.EndpointAdded += Agents.Add;
            this.Coordinator.EndpointRemoved += Agents.Remove;
            this.Coordinator.EndpointListUpdated += Agents.Update;
            this.Coordinator.Start();

            if (config.Monitor)
            {
                Monitor = new DispatcherMonitor(this, config.WebPort);
            }

            var hostUrl = Permissions.GetHostUrl(Config.DispatcherPort);
            host = WebApp.Start(new StartOptions(hostUrl)
            {
                AppStartup = typeof(DispatcherStartup).FullName
            });
            Console.WriteLine("Server running on {0}", hostUrl);
        }

        private void OnJobCompleted() => StartNextJob();

        public void AddJob(ITaskProvider taskProvider, int priority = 0)
        {
            lock (jobQueue)
            {
                jobQueue.Add(priority, new Job(this, taskProvider, priority));

                if (ActiveJob == null)
                {
                    StartNextJob();
                }
            }
        }

        private void StartNextJob()
        {
            lock (jobQueue)
            {
                if (ActiveJob != null)
                {
                    ActiveJob.Completed -= OnJobCompleted;
                }

                if (jobQueue.Count > 0)
                {
                    ActiveJob = jobQueue.Values[0];
                    jobQueue.RemoveAt(0);

                    ActiveJob.Start();
                    ActiveJob.Completed += OnJobCompleted;
                }
                else
                {
                    ActiveJob = null;
                }

                ActiveJobChanged?.Invoke(ActiveJob);
            }
        }

        internal void DiscardAgent(string agentId)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            Agents.Dispose();
            Coordinator.Stop();

            Monitor?.Dispose();

            if (ActiveJob != null)
            {
                ActiveJob.Completed -= OnJobCompleted;
                ActiveJob = null;
            }

            host.Dispose();
        }
    }
}
