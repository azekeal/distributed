using Distributed.Internal;

namespace Distributed
{
    public class AgentConfig
    {
        public int AgentPort = Constants.Ports.AgentHost;
        public string CoordinatorAddress = $"localhost:{Constants.Ports.CoordinatorHost}";
    }
}
