namespace Distributed.Internal
{
    public static class Constants
    {
        public static class Names
        {
            public const string Agent = "agent";
            public const string Dispatcher = "dispatcher";
            public const string Coordinator = "coordinator";
        }

        public static class Ports
        {
            public const int CoordinatorHost = 9000;
            public const int DispatcherHost = 9010;
            public const int AgentHost = 9020;
        }
    }
}
