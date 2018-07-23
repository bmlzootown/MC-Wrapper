using System;
using System.Drawing;
using Console = Colorful.Console;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Timers;
using System.Threading;
using System.Diagnostics;
using static Wrapper.Program;
 
namespace Wrapper {
    class Server {
        static System.Timers.Timer warnTimer;
        static System.Timers.Timer restartTimer;
        static DateTime restartTime;
        static Process mcproc;
        static StreamWriter input;

        public static Boolean willRestart;

        static Boolean running = true;
        
        // Setup restart timer
        public static void SetupRestart() {
            if (restartTimer != null) {
                restartTimer.Stop();
                restartTimer.Dispose();
            }

            willRestart = true;
            
            int sec = ((h*60)*60) + (m*60) + s;
            restartTimer = new System.Timers.Timer(sec*1000);
            restartTimer.Elapsed += new ElapsedEventHandler(OnRestartEvent);
            restartTimer.AutoReset = false;
            restartTimer.Enabled = true;

            restartTime = DateTime.Now;
            restartTime = restartTime.AddSeconds(sec);
        }

        private static void OnRestartEvent(Object source, ElapsedEventArgs e) {
            willRestart = true;
            stop();
            ((System.Timers.Timer)source).Dispose();
        }

        public static TimeSpan TimeRemaining() {
            return restartTime - DateTime.Now;
        }

        // Setup warning timers
        public static void SetupWarnings() {
            if (warnTimer != null) {
                warnTimer.Stop();
                warnTimer.Dispose();
            }

            warnTimer = new System.Timers.Timer(1000);
            warnTimer.Elapsed += new ElapsedEventHandler(onWarnEvent);
            warnTimer.AutoReset = true;
            warnTimer.Enabled = true;
        }

        private static void onWarnEvent(Object source, ElapsedEventArgs e) {
            for (int i = 0; i < warnTimes.Length; i++) {
                int tr = (int)TimeRemaining().TotalSeconds;
                int time = warnTimes[i];
                if (tr == time) {
                    if (time > 59) {
                        int minute = (time % 3600) / 60;
                        if (minute > 59) {
                            int hour = time / 3600;
                            sendCommand(warnCommand + " Restarting server in " + hour + " hours!");
                        } else {
                            sendCommand(warnCommand + " Restarting server in " + minute + " minutes!");
                        }
                    } else {
                        sendCommand(warnCommand + " Restarting server in " + time + " seconds!");
                    }
                }
            }
        }

        // Stops timers
        public static void StopTimers() {
            if (restartTimer != null) {
                restartTimer.Stop();
                restartTimer.Dispose();
            }
            if (warnTimer != null) {
                warnTimer.Stop();
                warnTimer.Dispose();
            }
        }

        // ... Stop wrapper after 2 second delay, obviously?
        public static void StopWrapper() {
            Thread.Sleep(2000);
            Environment.Exit(0);
        }

        // ... Starts the server? -.-
        public static void StartServer() {
            running = true;
            willRestart = false;
            if (!File.Exists("../" + jar)) {
                error("Server jar not found!");
                StopWrapper();
            }

            if (restart) SetupRestart();
            if (warn) SetupWarnings();

            ProcessStartInfo ServerInfo = new ProcessStartInfo(@"java.exe", @"" + jvm + " -jar " + jar + " " + options);
            ServerInfo.WorkingDirectory = @"../";
            ServerInfo.UseShellExecute = false;
            ServerInfo.RedirectStandardOutput = true;
            ServerInfo.RedirectStandardInput = true;
            ServerInfo.RedirectStandardError = true;

            mcproc = new Process();
            
            mcproc.StartInfo = ServerInfo;
            mcproc.EnableRaisingEvents = true;

            mcproc.OutputDataReceived += onReceiveData;
            mcproc.ErrorDataReceived += onReceiveError;
            mcproc.Exited += onExit;

            mcproc.Start();

            input = mcproc.StandardInput;
            input.AutoFlush = true;

            mcproc.BeginOutputReadLine();
            mcproc.BeginErrorReadLine();

            while(running) {
                var input = Console.ReadLine();
                string[] args = input.Split(' ');
                if (args[0].Equals(".stop", StringComparison.OrdinalIgnoreCase)) {
                    running = false;
                    willRestart = false;
                    stop();
                    mcproc.Close();
                    Thread.Sleep(1000);
                    Environment.Exit(0);
                } else if (args[0].Equals(".restart", StringComparison.OrdinalIgnoreCase)) {
                    //running = false;
                    willRestart = true;
                    stop();
                } else if (args[0].Equals(".cancel", StringComparison.OrdinalIgnoreCase)) {
                    StopTimers();
                    willRestart = false;
                    log("Restart canceled...");
                } else if (args[0].Equals(".reschedule", StringComparison.OrdinalIgnoreCase)) {
                    if (args.Length < 4 || args.Length > 4) {
                        log("Syntax Error: .reschedule [h|m|s]");
                    } else {
                        h = int.Parse(args[1]);
                        m = int.Parse(args[2]);
                        s = int.Parse(args[3]);
                        StopTimers();
                        SetupRestart();
                        if (warn) {
                            SetupWarnings();
                        }
                        log("Restart scheduled for " + h + "h " + m + "m " + s + "s");
                    }
                } else if (args[0].Equals(".status", StringComparison.OrdinalIgnoreCase)) {
                    if (!willRestart) {
                        log("No restart scheduled...");
                    } else {
                        TimeSpan time = TimeRemaining();
                        int diff = (int)time.TotalSeconds;
                        int hours = diff / 3600;
                        int minutes = (diff % 3600) / 60;
                        int seconds = diff % 60;
                        log("Remaining time: " + hours + "h " + minutes + "m " + seconds + "s");
                    }
                } else if (args[0].Equals(".reload", StringComparison.OrdinalIgnoreCase)) {
                    LoadConfig();
                    log("Config reloaded...");
                } else if (args[0].Equals(".enable", StringComparison.OrdinalIgnoreCase)) {
                    if (args.Length == 2) {
                        string plugin = args[1];
                        if (plugin.Contains(".jar")) {
                            plugin = plugin.Substring(0, plugin.Length - 4);
                        }
                        if (File.Exists("../plugins/" + plugin + ".disabled")) {
                            File.Create("../wrapper/updates/" + plugin + ".enable").Close();
                            log("Enabling " + plugin);
                        }
                    } else {
                        log("Syntax Error: .enable [plugin]");
                    }
                } else if (args[0].Equals(".disable", StringComparison.OrdinalIgnoreCase)) {
                    if (args.Length == 2) {
                        string plugin = args[1];
                        if (plugin.Contains(".jar")) {
                            plugin = plugin.Substring(0, plugin.Length - 4);
                        }
                        if (File.Exists("../plugins/" + plugin + ".jar")) {
                            File.Create("../wrapper/updates/" + plugin + ".disable").Close();
                            log("Disabling " + plugin);
                        }
                    } else {
                        log("Syntax Error: .disable [plugin]");
                    }
                } else if (args[0].Equals(".remove", StringComparison.OrdinalIgnoreCase)) {
                    if (args.Length == 2) {
                        string plugin = args[1];
                        if (plugin.Contains(".jar")) {
                            plugin = plugin.Substring(0, plugin.Length - 4);
                        }
                        if (File.Exists("../plugins/" + plugin + ".disabled") || File.Exists("../plugins/" + plugin + ".jar")) {
                            File.Create("../wrapper/updates/" + plugin + ".remove").Close();
                            log("Removing " + plugin);
                        }
                    } else {
                        log("Syntax Error: .remove [plugin]");
                    }
                } else if (args[0].Equals(".help", StringComparison.OrdinalIgnoreCase)) {
                    log(".status - Shows time remaining until next restart");
                    log(".restart - Restarts server");
                    log(".reschedule [h|m|s] - Reschedules restart");
                    log(".cancel - Cancels restart");
                    log(".stop - Stops server and wrapper");
                    log(".enable - Enables a disabled plugin on next restart");
                    log(".disable - Disables plugin on next restart");
                    log(".remove - Removes plugin on next restart");
                    log(".help - Shows all wrapper commands");
                } else {
                    sendCommand(input);
                }
            }
        }

        // Logs server output to console
        public static void onReceiveData(object sender, DataReceivedEventArgs e) {
            Console.WriteLine(e.Data);
        }

        //Error messages received from server, make 'em all red!
        public static void onReceiveError(object sender, DataReceivedEventArgs e) {
            if (e.Data != null) {
                error(e.Data);
            }
        }

        // On MC server stopping
        public static void onExit(object sender, EventArgs e) {
            if (debug) log("Server stopped...");

            // If not restarting, kill it with fire!
            if (!willRestart) {
                Environment.Exit(0);
            }
            if (running) running = false;

            // Handle updates
            HandleUpdates();

            // Wait before restart
            int t = wait;
            log("Restarting in...");
            while (t > 0) {
                log(t + "");
                t--;
                Thread.Sleep(1000);
            }
            
            // Loops back to starting server
            StartServer();
        }

        // Sends stop command to server
        public static void stop() {
            sendCommand(stopCommand);
        }

        // Sends command to server
        public static void sendCommand(string cmd) {
            if (input != null) {
                input.WriteLine(cmd);
            }
        }

        // Logging stuff
        public static void log(string log) {
            Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + " INFO]: " + log);
        }

        public static void error(string error) {
            Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + " ERROR]: " + error, Color.Red);
        }
    }
}
