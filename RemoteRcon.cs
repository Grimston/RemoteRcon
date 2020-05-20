using System;
using System.Linq;
using Oxide.Core.Libraries.Covalence;
using WebSocketSharp;


namespace Oxide.Plugins
{
    [Info("RemoteRcon", "Grimston", "0.0.9")]
    [Description("API to execute remote rcon commands to other servers.")]
    class RemoteRcon : CovalencePlugin
    {
        private WebSocket _webSocket;
        private Configuration _config;

        void Init()
        {
            _config = Config.ReadObject<Configuration>();
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(GetDefaultConfig(), true);
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                LogMessages = true,
                RemoteServers = new[]
                {
                    new Configuration.RemoteServer()
                    {
                        Name = "Local",
                        Address = "127.0.0.1",
                        Port = "28016",
                        Password = "YourSuperRconPassword"
                    },
                    new Configuration.RemoteServer()
                    {
                        Name = "Local_Duplicate",
                        Address = "127.0.0.1",
                        Port = "28016",
                        Password = "YourSuperRconPassword"
                    }
                },
                RemoteCommands = new[]
                {
                    new Configuration.RemoteCommand()
                    {
                        CommandName = "AddVIP",
                        Servers = new[] {"Local", "Local_Duplicate"},
                        Commands = new[]
                        {
                            "oxide.group add vip",
                            "oxide.usergroup add {0} vip"
                        }
                    },
                }
            };
        }

        private string FilterCommand(string command, string[] args)
        {
            /**
            var regex = new Regex(@"{(.*?)}/g");

            var matches = regex.Matches(command);

            foreach (Match match in matches)
            {
                //TODO: Process the command and create filters.
            }
            **/

            return string.Format(command, args);
        }

        [Command("remotercon.storedexecute"), Permission("remotercon.storedexecute")]
        void StoredExecute(IPlayer player, string command, string[] args)
        {
            try
            {
                if (!player.IsServer || !player.IsAdmin)
                {
                    return;
                }

                if (_config.LogMessages)
                    Puts("Checking for stored command: {0}", args[0]);

                var storedCommand = _config.RemoteCommands.First(remoteCommand => remoteCommand.CommandName == args[0]);

                if (storedCommand == null) return;

                if (_config.LogMessages)
                    Puts("Command Exists, running now");
                foreach (var storedServer in storedCommand.Servers)
                {
                    var identifier = 1000;
                    var storedRemoteServer =
                        _config.RemoteServers.First(remoteServer => remoteServer.Name == storedServer);

                    using (_webSocket =
                        new WebSocket(
                            $"ws://{storedRemoteServer.Address}:{storedRemoteServer.Port}/{storedRemoteServer.Password}")
                    )
                    {
                        _webSocket.Connect();

                        var commandArguments = args.ToList();
                        commandArguments.RemoveRange(0, 1);


                        if (_webSocket.ReadyState == WebSocketState.Open)
                        {
                            foreach (var storedCommandCommand in storedCommand.Commands)
                            {
                                var filterCommand = Newtonsoft.Json.JsonConvert.SerializeObject(new RconPacket()
                                {
                                    Identifier = identifier,
                                    Message = FilterCommand(storedCommandCommand, commandArguments.ToArray())
                                });

                                if (_config.LogMessages)
                                    Puts("Sending: {0}", filterCommand);
                                _webSocket.Send(filterCommand);

                                identifier++;
                            }
                        }
                        else
                        {
                            if (_config.LogMessages)
                                Puts($"Unable to connect to server: '{storedServer}' Commands not executed.");
                        }

                        _webSocket.Close();
                    }
                }
            }
            catch (Exception e)
            {
                Puts(e.ToString());
            }
        }

        [Command("remotercon.execute"), Permission("remotercon.execute")]
        void ExecuteCommand(IPlayer player, string command, string[] args)
        {
            try
            {
                if (!player.IsServer || !player.IsAdmin)
                {
                    return;
                }

                using (_webSocket =
                    new WebSocket(
                        $"ws://{args[0]}:{args[1]}/{args[2]}")
                )
                {
                    _webSocket.Connect();

                    var commandArguments = args.ToList();
                    commandArguments.RemoveRange(0, 1);

                    if (_webSocket.ReadyState == WebSocketState.Open)
                    {
                        var parts = args.ToList();
                        parts.RemoveRange(0, 3);

                        var filterCommand = Newtonsoft.Json.JsonConvert.SerializeObject(new RconPacket()
                        {
                            Identifier = 1000,
                            Message = string.Join(" ", parts)
                        });

                        if (_config.LogMessages)
                            Puts("Sending: {0}", filterCommand);
                        _webSocket.Send(filterCommand);
                    }
                    else
                    {
                        if (_config.LogMessages)
                            Puts($"Unable to connect to server, commands not executed.");
                    }

                    _webSocket.Close();
                }
            }
            catch (Exception e)
            {
                Puts(e.ToString());
            }
        }

        private class RconPacket
        {
            public int Identifier;
            public string Message;
            public string Name = "WebRcon";
        }

        private class Configuration
        {
            internal class RemoteCommand
            {
                public string CommandName;
                public string[] Servers;
                public string[] Commands;
            }

            internal class RemoteServer
            {
                public string Name;
                public string Address;
                public string Port;
                public string Password;
            }

            public bool LogMessages;
            public RemoteServer[] RemoteServers;
            public RemoteCommand[] RemoteCommands;
        }
    }
}
