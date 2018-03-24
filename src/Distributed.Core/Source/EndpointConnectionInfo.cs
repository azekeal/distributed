namespace Distributed.Core
{
    public struct EndpointConnectionInfo
    {
        public string name;
        public string signalrUrl;
        public string webUrl;

        public EndpointConnectionInfo(string name, string signalrUrl, string webUrl)
        {
            this.name = name;
            this.signalrUrl = signalrUrl;
            this.webUrl = webUrl;
        }

        public override string ToString() => $"[name:{name}, signalrUrl:{signalrUrl}, webUrl:{webUrl}]";
    }
}
