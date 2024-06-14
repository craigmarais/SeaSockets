using CSockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ClientSocket
{
    public class StreamClientConnection
    {
        CancellationToken _token;
        CClientSocket _socket;
        Thread _thread;

        object _sync = new();
        int _count = 0;
        long _sum = 0;

        public StreamClientConnection(string ip, ushort port, CancellationToken token)
        {
            _token = token;
            _socket = new CClientSocket(ip, port, _token);
            _socket.OnConnected += HandleConnected;
            _socket.OnMessageReceived += HandlePing;
            _socket.OnError += HandleError;
            _socket.Connect();

            _thread = new Thread(Run);
        }

        public void Start()
        {
            _thread.Start();
        }

        void Run()
        {
            while (!_token.IsCancellationRequested)
            {
                Ping();
            }
        }

        public ValueTuple<int, long> ReadMetrics()
        {
            lock (_sync)
            {
                var result = (_count, _sum);
                _count = 0;
                _sum = 0;
                return result;
            }
        }

        void Ping()
        {
            var time = new long[] { DateTime.Now.Ticks }.AsSpan();
            var timeBytes = MemoryMarshal.Cast<long, byte>(time);
            var bytesToSend = Utilities.AddLength(timeBytes.ToArray());
            _socket.Send(bytesToSend);
        }

        void HandlePing(Memory<byte> data, string endpoint)
        {
            var timeSent = MemoryMarshal.Cast<byte, long>(data.Span)[0];
            var diff = DateTime.Now.Ticks - timeSent;

            lock (_sync)
            {
                _sum += diff;
                _count++;
            }
        }

        void HandleConnected()
        {
            Console.WriteLine($"Connected to server on {_socket.Endpoint}");
        }

        void HandleError(string error, string endpoint)
        {
            string connectionState = _socket.Connected ? "maintained" : "Broken";
            Console.WriteLine($"Error on {endpoint} connection. Remote connection is {connectionState}. Error: {error}");
        }

    }
}
