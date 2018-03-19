using Distributed;
using System;

namespace Mock
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
