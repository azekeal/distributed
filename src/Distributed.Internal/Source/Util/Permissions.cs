using System;
using System.Security.Principal;

namespace Distributed.Internal.Util
{
    public static class Permissions
    {
        public static bool IsAdministrator() => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

        public static string GetHostUrl(int port)
        {
            if (IsAdministrator())
            {
                return $"http://*:{port}/";
            }
            else
            {
                Console.WriteLine("WARNING: Program needs to be run with admin permissions to be able to serve other computers.");
                return $"http://localhost:{port}/";
            }
        }
    }
}
