using Common;
using System;
using System.Collections.Generic;

namespace Dispatcher
{
    public class Config
    {
        public bool spawnAgent = true;
        public string initializationConfig;
    }

    public class Dispatcher : IDisposable
    {
        public string Identifier { get; private set; }
        public string EndpointData { get; private set; }
        public Config Config { get; private set; }
        public Coordinator Coordinator { get; private set; }
        public Job ActiveJob { get; private set; }
        public event Action<Job> ActiveJobChanged;

        private AgentPool agents;
        private SortedList<int, Job> jobQueue;

        public Dispatcher(Config config)
        {
            Identifier = $"{Constants.Names.Dispatcher}_{Guid.NewGuid()}";
            EndpointData = $"127.0.0.1:{Constants.Ports.DispatcherHost}";
            Config = config;

            agents = new AgentPool(this);
            jobQueue = new SortedList<int, Job>();

            Coordinator = new Coordinator($"http://localhost:{Constants.Ports.CoordinatorHost}/signalr", Identifier, EndpointData, "DispatcherHub");
            Coordinator.EndpointAdded += agents.Add;
            Coordinator.EndpointRemoved += agents.Remove;
            Coordinator.EndpointListUpdated += agents.Update;
            Coordinator.Start();
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

        internal void DiscardAgent(Agent agent)
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
