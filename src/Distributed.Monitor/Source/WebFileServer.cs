using System;
using System.IO;

namespace Distributed.Monitor
{
    public class WebFileServer : IDisposable
    {
        private WebServer webServer;

        public WebFileServer(string prefix, string directory, string defaultPath = "index.html")
        {
            Console.WriteLine($"Webserver started at {prefix}");
            webServer = new WebServer(new string[] { prefix }, request =>
            {
                var url = request.RawUrl;
                if (url == "/")
                {
                    url = defaultPath;
                }

                var path = Path.GetFullPath(directory + "/" + url);
                if (path.StartsWith(directory))
                {
                    if (File.Exists(path))
                    {
                        return File.ReadAllText(path);
                    }
                }

                return "";
            });

            webServer.Run();
        }

        public void Dispose()
        {
            webServer.Stop();
        }
    }
}
