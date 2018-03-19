using Distributed.Internal;

namespace Distributed
{
    public class DispatcherConfig
    {
        public int DispatcherPort = Constants.Ports.DispatcherHost;
        public int WebPort = Constants.Ports.DispatcherWebHost;
        public string CoordinatorAddress = $"localhost:{Constants.Ports.CoordinatorHost}";
        public bool Monitor = true;
    }
}
