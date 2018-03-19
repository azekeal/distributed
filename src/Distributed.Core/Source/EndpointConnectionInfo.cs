namespace Distributed.Core
{
    public struct EndpointConnectionInfo
    {
        public string name;

        public EndpointConnectionInfo(string name)
        {
            this.name = name;
        }

        public override string ToString() => name;
    }
}
