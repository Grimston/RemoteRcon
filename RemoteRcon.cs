using System;
using System.Threading;
using Newtonsoft.Json;
using WebSocketSharp;

namespace Oxide.Plugins
{
    [Info("RemoteRcon", "Grimston", "0.0.1")]
    [Description("API to execute remote rcon commands to other servers.")]
    class RemoteRcon : CovalencePlugin
    {
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
                Puts(e.Message);
            }
        }

        public static void Connect()
        {
            if (webSocket == null)
            {
                Puts("Can't connect, WebSocket is null");
                return;
            }

            Puts("WebSocket connecting");
            webSocket.ConnectAsync();
        }

        public static void Disconnect() => webSocket.CloseAsync();

        public static void Send(string packet)
        {
            if (webSocket.ReadyState == WebSocketState.Open)
            {
                webSocket.SendAsync(packet, null);
                Puts("Packet sent: " + packet);
            }
            else
            {
                Puts("WebSocket connection not ready, not sending packet");
            }
        }

        #endregion

        #region Events

        private static void OnClose(object sender, CloseEventArgs e)
        {
            Puts("WebSocket connection closed: " + e.Reason);
        }

        private static void OnError(object sender, ErrorEventArgs e)
        {
            Puts(e.Message);
        }

        private static void OnMessage(object sender, MessageEventArgs e)
        {
            Response response = JsonConvert.DeserializeObject<Response>(e.Data);

            if (response.Identifier == -1 || string.IsNullOrWhiteSpace(response.Message))
                return;

            Console.Write(response.Message);
        }

        private static void OnOpen(object sender, EventArgs e)
        {
            Puts("WebSocket connection opened");
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
