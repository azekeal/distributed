using Distributed.Core;
using Distributed.Internal;
using Distributed.Internal.Dispatcher;
using Distributed.Internal.Server;
using Distributed.Internal.Util;
using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Distributed.Monitor
{
    public class DispatcherMonitor : IDisposable
    {
        private ClientConnectionHandler connections;
        private IHubContext context;
        private Dispatcher dispatcher;
        private WebFileServer webFileServer;

        public DispatcherMonitor(Dispatcher dispatcher, int port)
        {
            this.dispatcher = dispatcher;

            dispatcher.ActiveJobChanged += OnActiveJobChanged;
            dispatcher.Coordinator.EndpointAdded += OnAgentAdded;
            dispatcher.Coordinator.EndpointListUpdated += OnAgentListUpdated;

            context = GlobalHost.ConnectionManager.GetHubContext<MonitorHub>();
            connections = new ClientConnectionHandler(context);

            var hostUrl = Permissions.GetHostUrl(port);
            webFileServer = new WebFileServer(hostUrl, Path.Combine(Environment.CurrentDirectory, @"Content\Dispatcher"));
        }

        private void OnAgentListUpdated(IEnumerable<EndpointConnectionInfo> list)
        {
            foreach (var info in list)
            {
                OnAgentAdded(info);
            }
        }

        private void OnAgentAdded(EndpointConnectionInfo info) => AgentAdded(info, context.Clients.All);
        private void OnAgentCapacityChanged(Agent agent, int capacity) => AgentCapacityChanged(agent, capacity, context.Clients.All);
        private void OnAgentTasksChanged(Agent agent, TaskState state, IEnumerable<TaskItem> tasks) => AgentTasksChanged(agent, state, tasks, context.Clients.All);


        private void OnActiveJobChanged(Job obj) => UpdateJob(context.Clients.All);

        public void OnConnect(dynamic caller)
        {
            UpdateJob(caller);

            foreach (var agentId in dispatcher.Agents.Keys)
            {
                var agent = dispatcher.Agents[agentId];

                AgentAdded(agent.Info, caller);
                AgentCapacityChanged(agent, agent.Capacity, caller);

                var pending = agent.PendingTasks;
                if (pending.Any())
                {
                    AgentTasksChanged(agent, TaskState.Pending, pending, caller);
                }

                var active = agent.ActiveTasks;
                if (active.Any())
                {
                    AgentTasksChanged(agent, TaskState.Active, active, caller);
                }
            }
        }

        private void AgentTasksChanged(Agent agent, TaskState state, IEnumerable<TaskItem> tasks, dynamic caller)
        {
            caller.updateTask(agent.Identifier, state.ToString().ToLower(), tasks);
        }

        private void AgentCapacityChanged(Agent agent, int capacity, dynamic caller)
        {
            caller.setAgentCapacity(agent.Identifier, agent.Capacity);
        }
        
        private void AgentAdded(EndpointConnectionInfo info, dynamic caller)
        {
            var agent = dispatcher.Agents[info.name];
            agent.TaskStateChanged += OnAgentTasksChanged;
            agent.CapacityChanged += OnAgentCapacityChanged;
            agent.Disposed += OnAgentDisposed;

            caller.addAgent(info.name, info.signalrUrl);

            if (agent.Capacity > 0)
            {
                AgentCapacityChanged(agent, agent.Capacity, caller);
            }
        }

        private void OnAgentDisposed(Agent agent)
        {
            agent.TaskStateChanged -= OnAgentTasksChanged;
            agent.CapacityChanged -= OnAgentCapacityChanged;

            var pending = agent.PendingTasks;
            if (pending.Any())
            {
                AgentTasksChanged(agent, TaskState.Cancelled, pending, context.Clients.All);
            }

            var active = agent.ActiveTasks;
            if (active.Any())
            {
                AgentTasksChanged(agent, TaskState.Cancelled, active, context.Clients.All);
            }

            context.Clients.All.removeAgent(agent.Identifier);
        }

        public void UpdateJob(dynamic group)
        {
            var job = Dispatcher.Instance.ActiveJob;
            if (job != null)
            {
                group.setJob(job.Name, job.Priority, job.Config, job.TaskCount);
            }
            else
            {
                group.setJob("none", 0, null, 0);
            }
        }

        public void Dispose()
        {
            dispatcher.ActiveJobChanged -= OnActiveJobChanged;

            webFileServer.Dispose();
        }
    }

}
