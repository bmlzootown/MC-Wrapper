using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using static Wrapper.Program;

namespace Wrapper {
    class UDPServer {
        public static void StartServer() {
            Thread udpServer = new Thread(new ThreadStart(Run));
            udpServer.IsBackground = true;
            udpServer.Start();
        }

        public static void Run() {
            Console.WriteLine("Starting UDP server...");

            byte[] data = new byte[1024];
            IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 25569);
            UdpClient newsock = new UdpClient(ipep);

            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);

            while (true) {
                data = newsock.Receive(ref sender);
                if (debug) Console.WriteLine("[UDP] From: {0}, Message: {1}", sender.ToString(), Encoding.ASCII.GetString(data, 0, data.Length));

                Responder.Respond(newsock, data, sender);
            }
        }
    }
}
