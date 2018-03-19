namespace Distributed.Core
{
    public struct EndpointConnectionInfo
    {
        public string name;
        public string endpointData;

        public EndpointConnectionInfo(string name, string endpointData)
        {
            this.name = name;
            this.endpointData = endpointData;
        }

        public override string ToString() => $"{name}, {endpointData}";
    }
}
