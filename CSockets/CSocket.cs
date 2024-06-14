using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace CSockets
{
    public class CSocket
    {
        public delegate void MessageReceived(Memory<byte> data, string endpoint);
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
            _socket.NoDelay = true;
            _socket.Blocking = true;
            _socket.ReceiveBufferSize = 8192;
            _socket.SendBufferSize = 8192;
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
        }

        public void Connect()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.NoDelay = true;
            _socket.Blocking = true;
            _socket.Connect(_host, _port);
            _thread.Start();
        }

        public void Send(byte[] data)
        {
            _socket.Send(data);
        }

        //ref: https://github.com/SeanFellowes/SocketMeister/blob/master/src/Shared.SocketMeister/SocketServer.cs#L485
        private void Run()
        {
            var buffer = new byte[_socket.ReceiveBufferSize];
            while (!_token.IsCancellationRequested)
            {
                try
                {
                    // Read header
                    var count = _socket.Receive(buffer, 4, SocketFlags.None);

                    if (count == 0)
                        continue;

                    var length = MemoryMarshal.Cast<byte, int>(buffer.AsSpan()[..4])[0];
                    
                    // Read body
                    count = _socket.Receive(buffer, length, SocketFlags.None);

                    OnMessageReceived?.Invoke(buffer.AsMemory()[..length], _socket.RemoteEndPoint?.ToString());
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex.ToString(), _socket.RemoteEndPoint.ToString());
                }
            }
        }
    }
}
