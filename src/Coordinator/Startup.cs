using Microsoft.AspNet.SignalR;
using Owin;
using System;

namespace Coordinator
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var hubConfiguration = new HubConfiguration();
            hubConfiguration.EnableDetailedErrors = true;

            // Any connection or hub wire up and configuration should go here
            app.MapSignalR(hubConfiguration);

            // Make long polling connections wait a maximum of 5 seconds for a
            // response. When that time expires, trigger a timeout command and
            // make the client reconnect.
            GlobalHost.Configuration.ConnectionTimeout = TimeSpan.FromSeconds(4);

            // Wait a maximum of 6 seconds after a transport connection is lost
            // before raising the Disconnected event to terminate the SignalR connection.
            GlobalHost.Configuration.DisconnectTimeout = TimeSpan.FromSeconds(6);

            // For transports other than long polling, send a keepalive packet every
            // 1 seconds. 
            // This value must be no more than 1/3 of the DisconnectTimeout value.
            GlobalHost.Configuration.KeepAlive = TimeSpan.FromSeconds(2);

            //GlobalHost.HubPipeline.RequireAuthentication();
        }
    }
}
