using System.Net;
using System.Net.Sockets;

namespace CSockets
{
    public class CServerSocket
    {
        public delegate void NewConnection(string endpoint);
        public event NewConnection OnNewConnection;
        public event CSocket.MessageReceived OnMessageReceived;
        public event CSocket.ErrorOccured OnError;

        private readonly Socket _acceptingSocket;
        private readonly Thread _acceptingThread;
        private readonly ushort _port;
        private readonly CancellationToken _token;
        private readonly Dictionary<string, CSocket> _connectedClients = new();

        public CServerSocket(ushort port, CancellationToken token)
        {
            _token = token;
            _port = port;
            _acceptingSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _acceptingSocket.Bind(new IPEndPoint(IPAddress.Any, _port));
            _acceptingThread = new Thread(ListenThread);
        }

        public bool Connected(string endpoint) => _connectedClients.ContainsKey(endpoint);                

        public void Listen()
        {
            _acceptingThread.Start();
        }

        void ListenThread()
        {
            while (!_token.IsCancellationRequested)
            {
                _acceptingSocket.Listen(1000);
                var socket = _acceptingSocket.AcceptAsync();

                if (socket != null)
                {
                    var connection = new CSocket(socket.Result, _token);
                    _connectedClients.TryAdd(connection.Endpoint, connection);
                    connection.OnMessageReceived += OnMessageReceived;
                    connection.OnError += Error;
                    OnNewConnection?.Invoke(connection.Endpoint);
                }
            }
        }

        public void Send(byte[] data, string endpoint)
        {
            if (!_connectedClients.TryGetValue(endpoint, out var connection))
                return;

            connection.Send(data);
        }

        private void Error(string message, string endpoint)
        {
            if (_connectedClients.TryGetValue(endpoint, out var connection) && !connection.Connected)
                _connectedClients.Remove(endpoint);

            OnError?.Invoke(message, endpoint);
        }
    }
}
