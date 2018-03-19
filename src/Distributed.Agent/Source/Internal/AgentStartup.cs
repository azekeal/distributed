using Owin;

namespace Distributed.Internal
{
    public class AgentStartup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
        }
    }
}
