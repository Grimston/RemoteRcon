using System;
using System.Threading;
using Newtonsoft.Json;
using WebSocketSharp;


namespace Oxide.Plugins
{
    [Info("RemoteRcon", "Grimston", "0.0.2")]
    [Description("API to execute remote rcon commands to other servers.")]
    class RemoteRcon : CovalencePlugin
    {
        void Init()
        {
            Connection.OnLog += (sender, s) => { Puts(s); };
        }

        [ConsoleCommand("ExecuteCommand")]
        [Help("Lets you run RCON commands on remote servers.")]
        void ExecuteCommand(string url, int port, string password, string command)
        {
            Connection.webSocket?.Close();

            Connection.Initialise(Connection.ConnectionString(url, port, password));

            Connection.Connect();

            SpinWait.SpinUntil(() =>
                Connection.webSocket.ReadyState != WebSocketState.Connecting &&
                Connection.webSocket.ReadyState != WebSocketState.Closing);

            var request = new Request(command);
            Connection.Send(JsonConvert.SerializeObject(request));
        }
    }


    class Connection : IDisposable
    {
        #region Members

        public static WebSocket webSocket;

        #endregion

        public static event EventHandler<string> OnLog;

        #region Methods

        public static string ConnectionString(string address, int port, string password)
            => $"ws://{address}:{port}/{password}";

        public static void Initialise(string url)
        {
            try
            {
                webSocket = new WebSocket(url);

                webSocket.OnOpen += OnOpen;
                webSocket.OnMessage += OnMessage;
                webSocket.OnError += OnError;
                webSocket.OnClose += OnClose;
            }
            catch (Exception e)
            {
                OnLog?.Invoke(null, e.Message);
            }
        }

        public static void Connect()
        {
            if (webSocket == null)
            {
                OnLog?.Invoke(null, "Can't connect, WebSocket is null");
                return;
            }

            OnLog?.Invoke(null, "WebSocket connecting");
            webSocket.ConnectAsync();
        }

        public static void Disconnect() => webSocket.CloseAsync();

        public static void Send(string packet)
        {
            if (webSocket.ReadyState == WebSocketState.Open)
            {
                webSocket.SendAsync(packet, null);
                OnLog?.Invoke(null, "Packet sent: " + packet);
            }
            else
            {
                OnLog?.Invoke(null, "WebSocket connection not ready, not sending packet");
            }
        }

        #endregion

        #region Events

        private static void OnClose(object sender, CloseEventArgs e)
        {
            OnLog?.Invoke(null, "WebSocket connection closed: " + e.Reason);
        }

        private static void OnError(object sender, ErrorEventArgs e)
        {
            OnLog?.Invoke(null, e.Message);
        }

        private static void OnMessage(object sender, MessageEventArgs e)
        {
            var response = JsonConvert.DeserializeObject<Response>(e.Data);

            if (response.Identifier == -1 || string.IsNullOrWhiteSpace(response.Message))
                return;

            Console.Write(response.Message);
        }

        private static void OnOpen(object sender, EventArgs e)
        {
            OnLog?.Invoke(null, "WebSocket connection opened");
        }

        #endregion

        public void Dispose()
        {
            webSocket.Close();
        }
    }

    class Request
    {
        public int Identifier;
        public string Message;
        public string Name;

        public Request(string message, int identifier = 1, string name = "WebRcon")
        {
            Identifier = identifier;
            Message = message;
            Name = name;
        }
    }

    class Response
    {
        [JsonProperty("Identifier")] public int Identifier;

        [JsonProperty("Message")] public string Message;

        [JsonProperty("Name")] public string Name;
    }
}
