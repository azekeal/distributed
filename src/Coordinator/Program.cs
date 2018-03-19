using Distributed;
using System;

namespace Mock
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new CoordinatorConfig();

            if (args.Length > 0)
            {
                config.CoordinatorPort = int.Parse(args[1]);
                Console.WriteLine($"CoordinatorHost: http://localhost:{config.CoordinatorPort}/");
            }

            using (var coordinator = new Coordinator(config))
            {
                Console.ReadLine();
            }
        }
    }
}
