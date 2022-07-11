using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace HTTPServer
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create a Http server and start listening for incoming connections
            HttpServer.listener = new HttpListener();
            HttpServer.listener.Prefixes.Add(HttpServer.url);
            HttpServer.listener.Start();
            Console.WriteLine("Listening for connections on {0}", HttpServer.url);

            // Handle requests
            Task listenTask = HttpServer.HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();

            // Close the listener
            HttpServer.listener.Close();
        }
    }
}
