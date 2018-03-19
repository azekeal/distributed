using Distributed.Core;
using Distributed.Internal;
using Distributed.Internal.Dispatcher;
using Distributed.Internal.Server;
using Distributed.Internal.Util;
using Microsoft.AspNet.SignalR;
using System;
using System.IO;

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
            dispatcher.Coordinator.EndpointRemoved += OnAgentRemoved;

            context = GlobalHost.ConnectionManager.GetHubContext<MonitorHub>();
            connections = new ClientConnectionHandler(context);

            var hostUrl = Permissions.GetHostUrl(port);
            webFileServer = new WebFileServer(hostUrl, Path.Combine(Environment.CurrentDirectory, @"Content\Dispatcher"));
        }

        private void OnAgentAdded(EndpointConnectionInfo info)
        {
            var agent = dispatcher.Agents[info.name];
            agent.TasksChanged += OnAgentTasksChanged;
        }

        private void OnAgentRemoved(string name)
        {
            var agent = dispatcher.Agents[name];
            if (agent != null)
            {
                agent.TasksChanged -= OnAgentTasksChanged;
            }
        }

        private void OnAgentTasksChanged(Agent agent, TaskItem[] pending, TaskItem[] active)
        {
            UpdateAgentTasks(agent, pending, active, context.Clients.All);
        }

        private void OnActiveJobChanged(Job obj) => UpdateJob(context.Clients.All);

        public void UpdateAgentTasks(Agent agent, TaskItem[] pending, TaskItem[] active, dynamic group)
        {
            group.setAgentTasks(agent.Identifier, pending, active);
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
