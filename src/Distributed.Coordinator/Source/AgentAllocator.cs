using Distributed.Core;
using Distributed.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Distributed
{
    public class AgentAllocator : IDisposable
    {
        private class DispatcherInfo
        {
            public string id;
            public string jobId;
            public EndpointConnectionInfo info;
        }

        private class AgentInfo
        {
            public string id;
            public string jobId;
            public EndpointConnectionInfo info;
        }

        private class JobInfo
        {
            public string id;
            public int priority;
            public int taskCount;

            public string dispatcherId;

            public int queuePriorityValue;
            public int maxAgents;
            public List<string> agents = new List<string>();
        }

        public event Action<string, EndpointConnectionInfo> DispatcherAssignAgent;
        public event Action<string, EndpointConnectionInfo> DispatcherRemoveAgent;

        private Dictionary<string, DispatcherInfo> dispatchers = new Dictionary<string, DispatcherInfo>();
        private Dictionary<string, AgentInfo> agents = new Dictionary<string, AgentInfo>();
        private Dictionary<string, JobInfo> jobs = new Dictionary<string, JobInfo>();
        private HashSet<string> unassignedAgents = new HashSet<string>();

        private SortedSet<JobInfo> unsaturatedJobQueue = new SortedSet<JobInfo>(Comparer<JobInfo>.Create((jobA, jobB) => jobB.queuePriorityValue - jobA.queuePriorityValue)); // descending
        private HashSet<JobInfo> saturatedJobSet = new HashSet<JobInfo>();
        private int totalTasks;

        private object lockObj = new object();

        // Agent created
        public void AddAgent(string agentId, EndpointConnectionInfo info)
        {
            // all public functions should have locks since we concurrent access
            lock (lockObj)
            {
                using (Trace.Log($"agentId: {agentId}, info: {info}"))
                {
                    if (agents.ContainsKey(agentId))
                    {
                        return;
                    }

                    var agent = new AgentInfo()
                    {
                        id = agentId,
                        jobId = null,
                        info = info,
                    };
                    agents[agentId] = agent;

                    if (!AssignAgentAsLoopback(agentId))
                    {
                        unassignedAgents.Add(agentId);
                        UpdateAllocations();
                    }
                }
            }
        }

        // Agent died
        public void RemoveAgent(string agentId)
        {
            // all public functions should have locks since we concurrent access
            lock (lockObj)
            {
                using (Trace.Log($"agentId: {agentId}"))
                {
                    AssignAgent(agentId, null);

                    unassignedAgents.Remove(agentId);
                    agents.Remove(agentId);

                    UpdateAllocations();
                }
            }
        }

        public void ClearJob(string dispatcherId)
        {
            // all public functions should have locks since we concurrent access
            lock (lockObj)
            {
                using (Trace.Log($"dispatcherId: {dispatcherId}"))
                {
                    if (dispatchers.TryGetValue(dispatcherId, out var dispatcher))
                    {
                        if (dispatcher.jobId != null)
                        {
                            UpdateJob(dispatcherId, dispatcher.jobId, 0, 0);

                            var job = jobs[dispatcher.jobId];
                            job.agents.ForEach(agentId => AssignAgent(unassignedAgents.First(), null));
                            jobs.Remove(dispatcher.jobId);
                            dispatcher.jobId = null;
                        }
                    }
                }
            }
        }

        public void UpdateJob(string dispatcherId, string jobId, int priority, int taskCount)
        {
            // all public functions should have locks since we concurrent access
            lock (lockObj)
            {
                using (Trace.Log($"dispatcherId: {dispatcherId}, jobId: {jobId}, priority: {priority}, taskCount: {taskCount}"))
                {
                    var prevTaskCount = 0;

                    if (dispatchers.TryGetValue(dispatcherId, out var dispatcher))
                    {
                        if (dispatcher.jobId != null && dispatcher.jobId != jobId)
                        {
                            ClearJob(dispatcherId);
                        }

                        dispatcher.jobId = jobId;
                    }
                    else
                    {
                        throw new Exception("Job doesn't belong to a registered dispatcher");
                    }

                    bool isNewJob = false;
                    if (!jobs.TryGetValue(jobId, out var job))
                    {
                        job = new JobInfo
                        {
                            dispatcherId = dispatcherId,
                            id = jobId
                        };
                        jobs[jobId] = job;

                        isNewJob = true;
                    }
                    else
                    {
                        prevTaskCount = job.taskCount;
                    }

                    job.priority = priority;
                    job.taskCount = taskCount;

                    // update total tasks
                    totalTasks += taskCount - prevTaskCount;

                    (job.queuePriorityValue, job.maxAgents) = CalculateJobPriority(job);

                    UpdateJobAgentSaturation(job);

                    if (isNewJob)
                    {
                        foreach (var agentId in GetLoopbackAgents(dispatcherId))
                        {
                            AssignAgentAsLoopback(agentId);
                        }
                    }

                    UpdateAllocations();
                }
            }
        }

        /// <summary>
        /// Dispatchers on the same machine are prioritized by the agents
        /// </summary>
        /// <param name="agentId"></param>
        /// <returns></returns>
        private bool AssignAgentAsLoopback(string agentId)
        {
            var dispatcherId = GetLoopbackDispatcher(agentId);
            if (dispatcherId == null)
            {
                return false;
            }

            var jobId = dispatchers[dispatcherId].jobId;
            if (jobId == null)
            {
                return false;
            }

            AssignAgent(agentId, jobId);
            return true;
        }

        private IEnumerable<string> GetLoopbackAgents(string dispatcherId)
        {
            if (dispatchers.TryGetValue(dispatcherId, out var dispatcher))
            {
                return agents.Where(pair => pair.Value.info.MatchesHost(dispatcher.info)).Select(pair => pair.Key);
            }

            return Enumerable.Empty<string>();
        }

        private string GetLoopbackDispatcher(string agentId)
        {
            if (!agents.TryGetValue(agentId, out var agent))
            {
                return null;
            }

            var dispatcher = dispatchers.Values.FirstOrDefault(d => d.info.MatchesHost(agent.info));
            if (dispatcher == null)
            {
                return null;
            }

            return dispatcher.id;
        }

        public void ReleaseAgent(string dispatcherId, string jobId, string agentId)
        {
            // all public functions should have locks since we concurrent access
            lock (lockObj)
            {
                using (Trace.Log($"dispatcherId: {dispatcherId}, jobId: {jobId}, agentId: {agentId}"))
                {
                    if (agents.TryGetValue(agentId, out var agent) && agent.jobId == jobId)
                    {
                        AssignAgent(agentId, null);
                    }
                }
            }
        }

        public void AddDispatcher(string dispatcherId, EndpointConnectionInfo info)
        {
            // all public functions should have locks since we concurrent access
            lock (lockObj)
            {
                using (Trace.Log($"dispatcherId: {dispatcherId}, info: {info}"))
                {
                    if (!dispatchers.TryGetValue(dispatcherId, out var dispatcher))
                    {
                        dispatcher = new DispatcherInfo
                        {
                            id = dispatcherId,
                            info = info
                        };

                        dispatchers[dispatcherId] = dispatcher;
                    }
                }
            }
        }

        public void RemoveDispatcher(string dispatcherId)
        {
            // all public functions should have locks since we concurrent access
            lock (lockObj)
            {
                using (Trace.Log($"dispatcherId: {dispatcherId}"))
                {
                    if (dispatchers.TryGetValue(dispatcherId, out var dispatcher))
                    {
                        ClearJob(dispatcherId);
                        dispatchers.Remove(dispatcherId);
                    }
                }
            }
        }

        private (int priority, int maxAgents) CalculateJobPriority(JobInfo job)
        {
            // TODO: expand ruleset
            var priority = 0;

            // priority has large effect
            priority += job.priority * 1000;
            
            // slight priority for larger jobs
            priority += (job.taskCount / 100);

            // max agents as ratio of number of tasks relative to total
            var maxAgents = 1 + (agents.Count * job.taskCount / totalTasks);

            return (priority, maxAgents);
        }

        private void NotifyDispatcherEvent(Action<string, EndpointConnectionInfo> handler, string dispatcherId, EndpointConnectionInfo info)
        {
            if (handler != null)
            {
                handler.Invoke(dispatcherId, info);
                //Task.Run(() => handler.Invoke(dispatcherId, info));
            }
        }

        private void AssignAgent(string agentId, string jobId)
        {
            using (Trace.Log($"agentId: {agentId}, jobId: {jobId}"))
            {
                if (agents.TryGetValue(agentId, out var agent))
                {
                    if (agent.jobId != null)
                    {
                        var job = jobs[agent.jobId];
                        job.agents.Remove(agentId);
                        UpdateJobAgentSaturation(job);

                        NotifyDispatcherEvent(DispatcherRemoveAgent, job.dispatcherId, agent.info);
                    }
                    else
                    {
                        unassignedAgents.Remove(agentId);
                    }

                    agent.jobId = jobId;

                    if (agent.jobId != null)
                    {
                        var job = jobs[agent.jobId];
                        job.agents.Add(agentId);
                        UpdateJobAgentSaturation(job);

                        NotifyDispatcherEvent(DispatcherAssignAgent, job.dispatcherId, agent.info);
                    }
                    else
                    {
                        unassignedAgents.Add(agentId);
                    }
                }
            }
        }

        private void UpdateJobAgentSaturation(JobInfo job)
        {
            using (Trace.Log($"{job.id}"))
            {
                if (job.agents.Count < job.maxAgents)
                {
                    saturatedJobSet.Remove(job);
                    unsaturatedJobQueue.Add(job);
                }
                else
                {
                    unsaturatedJobQueue.Remove(job);
                    saturatedJobSet.Add(job);
                }
            }
        }

        private void UpdateAllocations()
        {
            using (Trace.Log($"unassignedAgents: {unassignedAgents.Count}, unsaturatedJobQueue: {unsaturatedJobQueue.Count}"))
            {
                // TODO: choose best agents based on heuristic (proximity, cache, etc)
                while (unassignedAgents.Count > 0 & unsaturatedJobQueue.Count > 0)
                {
                    var job = unsaturatedJobQueue.First();
                    while (job.agents.Count < job.maxAgents && unassignedAgents.Count > 0)
                    {
                        AssignAgent(unassignedAgents.First(), job.id);
                    }
                }
            }
        }

        public void Dispose()
        {
            using (Trace.Log())
            {
                lock (lockObj)
                {
                    // TODO: cleanup

                    DispatcherAssignAgent = null;
                    DispatcherRemoveAgent = null;
                }
            }
        }
    }
}
