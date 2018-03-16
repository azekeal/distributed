using Common;
using Microsoft.Owin.Hosting;
using System;

namespace Coordinator
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var coordinator = new Coordinator())
            {
                Console.ReadLine();
            }
        }
    }
}
