using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Configuration;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static Wrapper.Server;

namespace Wrapper {
    class Program {
        public static Boolean restart;
        public static Boolean warn;
        public static Boolean crash;
        public static Boolean debug;
        public static int h;
        public static int m;
        public static int s;
        public static int wait;
        public static int[] warnTimes;
        public static string stopCommand;
        public static string warnCommand;
        public static string jar;
        public static string jvm;
        public static string options;

        public static void InitConfig() {
            Directory.CreateDirectory("../wrapper/updates");
            if (!File.Exists("../wrapper/config.yml")) {
                var stream = new YamlStream(
                    new YamlDocument(
                        new YamlMappingNode(
                            new YamlScalarNode("restart"), new YamlMappingNode(
                                new YamlScalarNode("enable"), new YamlScalarNode("true"),
                                new YamlScalarNode("h"), new YamlScalarNode("4"),
                                new YamlScalarNode("m"), new YamlScalarNode("0"),
                                new YamlScalarNode("s"), new YamlScalarNode("0"),
                                new YamlScalarNode("wait"), new YamlScalarNode("5"),
                                new YamlScalarNode("warn"), new YamlMappingNode(
                                    new YamlScalarNode("enable"), new YamlScalarNode("true"),
                                    new YamlScalarNode("command"), new YamlScalarNode("say"),
                                    new YamlScalarNode("timings"), new YamlSequenceNode(
                                        new YamlScalarNode("1800"),
                                        new YamlScalarNode("600"),
                                        new YamlScalarNode("300"),
                                        new YamlScalarNode("60"),
                                        new YamlScalarNode("30")
                                    )
                                ),
                                new YamlScalarNode("crash"), new YamlMappingNode(
                                    new YamlScalarNode("enable"), new YamlScalarNode("true")
                                )
                            ),
                            new YamlScalarNode("stop-command"), new YamlScalarNode("stop"),
                            new YamlScalarNode("server-jar"), new YamlScalarNode("paperclip.jar"),
                            new YamlScalarNode("jvm-options"), new YamlScalarNode("-Xmx5120M"),
                            new YamlScalarNode("launch-options"), new YamlScalarNode("nogui"),
                            new YamlScalarNode("debug"), new YamlScalarNode("false")
                        )
                    )
                );

                using (TextWriter writer = File.CreateText("../wrapper/config.yml")) {
                    stream.Save(writer, false);
                }

                Console.WriteLine("Config not found... Creating!");
            } else {
                Console.WriteLine("Config already exists!");
            }
            LoadConfig();
        }

        public static void LoadConfig() {
            string contents = File.ReadAllText(@"../wrapper/config.yml");

            var input = new StringReader(contents);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();

            var ymlConfig = deserializer.Deserialize<YmlConfig>(input);

            restart = ymlConfig.Restart.Enable;
            warn = ymlConfig.Restart.Warn.Enable;
            crash = ymlConfig.Restart.Crash.Enable;
            debug = ymlConfig.Debug;
            h = ymlConfig.Restart.H;
            m = ymlConfig.Restart.M;
            s = ymlConfig.Restart.S;
            wait = ymlConfig.Restart.Wait;
            warnCommand = ymlConfig.Restart.Warn.Command;
            stopCommand = ymlConfig.StopCommand;
            jar = ymlConfig.ServerJar;
            jvm = ymlConfig.JvmOptions;
            options = ymlConfig.LaunchOptions;
            warnTimes = ymlConfig.Restart.Warn.Timings;
        }

        public static void HandleUpdates() {
            if (Directory.Exists("../wrapper/updates") && Directory.Exists("../plugins")) {
                if (debug) log("Attempting to enable/disable/remove/add plugins...");
                DirectoryInfo d = new DirectoryInfo(@"../wrapper/updates");
                DirectoryInfo pl = new DirectoryInfo(@"../plugins");
                FileInfo[] Files = d.GetFiles();
                foreach (FileInfo file in Files) {
                    String ext = file.Extension;
                    String name = Path.GetFileNameWithoutExtension(file.Name);
                    if (debug) log("Found file " + name + ext + " in update folder...");
                    if (ext.Equals(".remove", StringComparison.OrdinalIgnoreCase)) {
                        if (File.Exists("../plugins/" + name + ".jar")) {
                            string full = pl.FullName;
                            File.Delete(@full + "/" + name + ".jar");
                        }
                        file.Delete();
                        log("Removed plugin: " + name);
                    } else if (ext.Equals(".disable", StringComparison.OrdinalIgnoreCase)) {
                        if (File.Exists("../plugins/" + name + ".jar")) {
                            string full = pl.FullName;
                            File.Move(@full + "/" + name + ".jar", @full + "/" + name + ".disabled");
                        }
                        file.Delete();
                        log("Disabled plugin: " + name);
                    } else if (ext.Equals(".enable", StringComparison.OrdinalIgnoreCase)) {
                        if (File.Exists("../plugins/" + name + ".disabled")) {
                            string full = pl.FullName;
                            File.Move(@full + "/" + name + ".disabled", @full + "/" + name + ".jar");
                        }
                        file.Delete();
                        log("Enabled plugin: " + name);
                    } else if (ext.Equals(".jar", StringComparison.OrdinalIgnoreCase)) {
                        string full = pl.FullName + "/" + name + ext;
                        file.MoveTo(@full);
                        log("Added plugin: " + name);
                    }
                }
            }
        }

        // Whoops, I hid Main from myself... x_x
        static void Main(string[] args) {
            Console.WriteLine("WrappingPaper by bmlzootown");
            InitConfig();
            Thread.Sleep(1000);
            LoadConfig();
            Thread.Sleep(1000);
            UDPServer.StartServer();
            Thread.Sleep(1000);
            HandleUpdates();
            Thread.Sleep(1000);
            Server.StartServer();
            /*HandleUpdates();
            while (true) {
                var input = Console.ReadLine();
                if (input.Equals("break")) {
                    break;
                }
            }*/
        }

        public class YmlConfig {
            [YamlMember(Alias = "stop-command", ApplyNamingConventions = false)]
            public string StopCommand { get; set; }

            [YamlMember(Alias = "server-jar", ApplyNamingConventions = false)]
            public string ServerJar { get; set; }

            [YamlMember(Alias = "jvm-options", ApplyNamingConventions = false)]
            public string JvmOptions { get; set; }

            [YamlMember(Alias = "launch-options", ApplyNamingConventions = false)]
            public string LaunchOptions { get; set; }

            public Boolean Debug { get; set; }
            public Restart Restart { get; set; }
        }

        public class Restart {
            public Boolean Enable { get; set; }
            public int H { get; set; }
            public int M { get; set; }
            public int S { get; set; }
            public int Wait { get; set; }
            public Warn Warn { get; set; }
            public Crash Crash { get; set; }
        }

        public class Warn {
            public Boolean Enable { get; set; }
            public string Command { get; set; }
            public int[] Timings { get; set; }
        }

        public class Crash {
            public Boolean Enable { get; set; }
        }
    }
}
