using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AsyncTCP
{
    class Program
    {
        private const int BufferSize = 4096;
        private static readonly bool ServerRunning = true;

        static void Main(string[] args)
        {
            
            var tcpServer = new TcpListener(IPAddress.Parse("127.0.0.1"), 9000);
            try
            {
                tcpServer.Start();
                Console.WriteLine(tcpServer.LocalEndpoint.ToString());
                var x = ListenForClients(tcpServer);
                Console.WriteLine("Press enter to shutdown");
                x.Wait();
            }
            finally
            {
                tcpServer.Stop();
            }
        }

        private static async Task ListenForClients(TcpListener tcpServer)
        {
            while (ServerRunning)
            {
                var tcpClient = await tcpServer.AcceptTcpClientAsync();
                Console.WriteLine("Connected");
                ProcessWClient(tcpClient);
                ProcessClient(tcpClient);
                

            }
        }

        private static async Task ProcessClient(TcpClient tcpClient)
        {
            while (ServerRunning)
            {
                if (tcpClient.Client != null)
                {
                    var stream = tcpClient.GetStream();
                    var buffer = new byte[BufferSize];
                    var amountRead = await stream.ReadAsync(buffer, 0, BufferSize);
                    var message = Encoding.ASCII.GetString(buffer, 0, amountRead);
                    Console.WriteLine("Client sent: {0}", message);
                }
            }
        }

        private static async Task ProcessWClient(TcpClient tcpClient)
        {
            //while (ServerRunning)
            //{
            //    if (Console.KeyAvailable)
            //    {
            //        var x = Console.ReadLine();
            //        var stream = tcpClient.GetStream();

            //        byte[] buffer = Encoding.ASCII.GetBytes(x);
            //        await stream.WriteAsync(buffer, 0, buffer.Length);
            //        stream.Flush();
            //    }                // amountRead.Wait();
                
            //}
        }
    }
}
