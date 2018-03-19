namespace Distributed.Core
{
    public struct EndpointConnectionInfo
    {
        public string name;
        public string endpoint;

        public EndpointConnectionInfo(string name, string endpoint)
        {
            this.name = name;
            this.endpoint = endpoint;
        }

        public override string ToString() => name;
    }
}
