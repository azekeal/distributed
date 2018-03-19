using Distributed.Internal;

namespace Distributed
{
    public class AgentConfig
    {
        public int AgentPort = Constants.Ports.AgentHost;
        public int WebPort = Constants.Ports.AgentWebHost;
        public string CoordinatorAddress = $"localhost:{Constants.Ports.CoordinatorHost}";
    }
}
