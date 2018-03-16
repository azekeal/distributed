using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Coordinator
{
    class Program
    {
        // Singleton instance
        private readonly static Lazy<Program> instance = new Lazy<Program>(() => new Program());

        private IHubContext dispatcherHubContext;
        private IHubContext agentHubContext;

        private Program()
        {
            dispatcherHubContext = GlobalHost.ConnectionManager.GetHubContext<DispatcherHub>();
            agentHubContext = GlobalHost.ConnectionManager.GetHubContext<AgentHub>();
        }

        static void Main(string[] args)
        {
            // This will *ONLY* bind to localhost, if you want to bind to all addresses
            // use http://*:8080 to bind to all addresses. 
            // See http://msdn.microsoft.com/library/system.net.httplistener.aspx 
            // for more information.
            string url = "http://localhost:8080";
            using (WebApp.Start(url))
            {
                Console.WriteLine("Server running on {0}", url);
                Console.ReadLine();
            }
        }
    }
}
