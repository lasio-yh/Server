using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PushServiceConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Trace.Listeners.Clear();
                Trace.Listeners.Add(new ConsoleTraceListener());
                Trace.Listeners.Add(new TextWriterTraceListener(DateTime.Now.ToString("yyyyMMdd")));
                Trace.AutoFlush = true;
                Trace.TraceInformation(string.Format("{0}|{1}", DateTime.Now.ToString("hh:mm:ss"), "Starting Server"));
                var server = new PushServiceConsole.TCPServer(9000);
                server.StartServer();
                Console.ReadKey();
                Trace.TraceInformation(string.Format("{0}|{1}", DateTime.Now.ToString("hh:mm:ss"), "Closing Server"));
                server.StopServer();
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
            }
        }
    }
}
