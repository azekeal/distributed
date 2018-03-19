using Distributed.Internal;
using Distributed.Internal.Dispatcher;
using System;
using System.Collections.Generic;

namespace Distributed
{
    public sealed class Dispatcher : IDisposable
    {
        public string Identifier { get; private set; }
        public string EndpointData { get; private set; }
        public CoordinatorConnection Coordinator { get; private set; }
        public Job ActiveJob { get; private set; }
        public DispatcherConfig Config { get; private set; }

        public event Action<Job> ActiveJobChanged;

        private AgentPool agents;
        private SortedList<int, Job> jobQueue;

        public Dispatcher() : this(new DispatcherConfig()) { }

        public Dispatcher(DispatcherConfig config)
        {
            this.Identifier = $"{Constants.Names.Dispatcher}_{Guid.NewGuid()}";
            this.EndpointData = $"127.0.0.1:{Constants.Ports.DispatcherHost}";
            this.Config = config;

            this.agents = new AgentPool(this);
            this.jobQueue = new SortedList<int, Job>();

            this.Coordinator = new CoordinatorConnection($"http://{config.CoordinatorAddress}/signalr", Identifier, EndpointData, "DispatcherHub");
            this.Coordinator.EndpointAdded += agents.Add;
            this.Coordinator.EndpointRemoved += agents.Remove;
            this.Coordinator.EndpointListUpdated += agents.Update;
            this.Coordinator.Start();
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
            agents.Dispose();
            Coordinator.Stop();

            if (ActiveJob != null)
            {
                ActiveJob.Completed -= OnJobCompleted;
                ActiveJob = null;
            }
        }
    }
}
