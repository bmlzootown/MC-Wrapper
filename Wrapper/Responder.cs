using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using static Wrapper.Server;
using static Wrapper.Program;
using System.IO;

namespace Wrapper {
    class Responder {
        static UdpClient socket;
        static byte[] data;
        static IPEndPoint sender;

        public static void Respond(UdpClient sock,  byte[] d, IPEndPoint s) {
            socket = sock;
            data = d;
            sender = s;

            Thread udpServer = new Thread(new ThreadStart(Run));
            udpServer.IsBackground = true;
            udpServer.Start();
        }

        public static void Run() {
            String request = Encoding.ASCII.GetString(data, 0, data.Length).ToLowerInvariant();
            String[] args = request.Split(':');

            if (args[0].Equals("status")) {
                if (!willRestart) {
                    SendResponse("[NULL]:pasta");
                } else {
                    TimeSpan time = TimeRemaining();
                    int diff = (int)time.TotalSeconds;
                    int hours = diff / 3600;
                    int minutes = (diff % 3600) / 60;
                    int seconds = diff % 60;
                    string reply = hours + ":" + minutes + ":" + seconds;
                    SendResponse(reply);
                }
            } else if (args[0].Equals("reschedule")) {
                if (args.Length < 4 || args.Length > 4) {
                    log("UDP Syntax Error: .reschedule [h|m|s]");
                } else {
                    h = int.Parse(args[1]);
                    m = int.Parse(args[2]);
                    s = int.Parse(args[3]);
                    SetupRestart();
                    if (warn) {
                        SetupWarnings();
                    }
                    log("Restart scheduled for " + h + "h " + m + "m " + s + "s");
                }
            } else if (args[0].Equals("reload")) {
                LoadConfig();
                log("Config reloaded...");
            } else if (args[0].Equals("stop")) {
                willRestart = false;
            } else if (args[0].Equals("cancel")) {
                StopTimers();
                log("Restart canceled...");
            } else if (args[0].Equals("enable")) {
                if (args.Length == 2) {
                    string plugin = args[1];
                    if (plugin.Contains(".jar")) {
                        plugin = plugin.Substring(0, plugin.Length - 4);
                    }
                    if (File.Exists("../plugins/" + plugin + ".disabled")) {
                        File.Create("../wrapper/updates/" + plugin + ".enable").Close();
                    }
                    log("Queued enabling of: " + plugin);
                } else {
                    log("Syntax Error: .enable [plugin]");
                }
            } else if (args[0].Equals("disable")) {
                if (args.Length == 2) {
                    string plugin = args[1];
                    if (plugin.Contains(".jar")) {
                        plugin = plugin.Substring(0, plugin.Length - 4);
                    }
                    if (File.Exists("../plugins/" + plugin + ".jar")) {
                        File.Create("../wrapper/updates/" + plugin + ".disable").Close();
                    }
                    log("Queued disabling of: " + plugin);
                } else {
                    log("Syntax Error: .disable [plugin]");
                }
            } else if (args[0].Equals("remove")) {
                if (args.Length == 2) {
                    string plugin = args[1];
                    if (plugin.Contains(".jar")) {
                        plugin = plugin.Substring(0, plugin.Length - 4);
                    }
                    if (File.Exists("../plugins/" + plugin + ".disabled") || File.Exists("../plugins/" + plugin + ".jar")) {
                        File.Create("../wrapper/updates/" + plugin + ".remove").Close();
                    }
                    log("Queued remove of: " + plugin);
                } else {
                    log("Syntax Error: .remove [plugin]");
                }
            }
        }

        static void SendResponse(string response) {
            byte[] d = Encoding.ASCII.GetBytes(response);
            socket.Send(d, d.Length, sender);
        }
    }
}
