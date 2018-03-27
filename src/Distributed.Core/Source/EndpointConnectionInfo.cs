using System;

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

        public bool MatchesHost(EndpointConnectionInfo other)
        {
            return GetAbsoluteUri().Host.Equals(other.GetAbsoluteUri().Host, StringComparison.OrdinalIgnoreCase);
        }

        private Uri GetAbsoluteUri()
        {
            var url = signalrUrl;
            if (!url.StartsWith("http://"))
            {
                url = "http://" + url;
            }

            return new Uri(url);
        }
    }
}
