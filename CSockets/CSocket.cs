using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace CSockets
{
    public class CSocket
    {
        public delegate void MessageReceived(byte[] data, string endpoint);
        public MessageReceived OnMessageReceived;
        public delegate void ErrorOccured(string message, string endpoint);
        public ErrorOccured OnError;

        private Socket _socket;
        private readonly string _host;
        private readonly ushort _port;

        private Thread _thread;
        private readonly CancellationToken _token;

        public string Endpoint => $"{_socket.RemoteEndPoint}";
        public bool Connected => _socket?.Connected ?? false;

        public CSocket(Socket socket, CancellationToken token)
        {
            _socket = socket;
            _token = token;
            _thread = new Thread(Run);
            _thread.Start();
        }
        public CSocket(string host, ushort port, CancellationToken token)
        {
            _host = host;
            _port = port;
            _token = token;
            _thread = new Thread(Run);
            _thread.Start();
        }

        public void Connect()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Connect(_host, _port);
        }

        public void Send(byte[] data)
        {
            _socket.Send(data);
        }

        private async void Run()
        {
            while (!_token.IsCancellationRequested)
            {
                try
                {
                    if (_socket == null || _socket.Available == 0)
                        continue;

                    var bodyLength = await ReadHeaderAsync();
                    await ReadBodyAsync(bodyLength);
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex.ToString(), _socket.RemoteEndPoint.ToString());
                }
            }
        }

        private async Task<int> ReadHeaderAsync()
        {
            Memory<byte> header = new byte[4];
            await _socket.ReceiveAsync(header, SocketFlags.None, _token);
            return MemoryMarshal.Cast<byte,int>(header.Span)[0];
        }

        private async Task ReadBodyAsync(int length)
        {
            var buffer = new byte[length];
            await _socket.ReceiveAsync(buffer, SocketFlags.None, _token);
            OnMessageReceived?.Invoke(buffer, _socket.RemoteEndPoint?.ToString());
        }
    }
}
