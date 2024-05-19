using CSockets;
using System.Runtime.InteropServices;

CancellationTokenSource cts = new CancellationTokenSource();
var sync = new object();
int count = 0;
long sum = 0;

new Thread(LogLatencyThread).Start();

CClientSocket _socket = new CClientSocket("127.0.0.1", 7324, cts.Token);
_socket.OnConnected += HandleConnected;
_socket.OnMessageReceived += HandlePing;
_socket.OnError += HandleError;

_socket.Connect();
Ping();

KeepAlive(cts);

void HandlePing(byte[] data, string endpoint)
{
    var timeSent = MemoryMarshal.Cast<byte, long>(data.AsSpan())[0];
    var diff = DateTime.Now.Ticks - timeSent;

    lock (sync)
    {
        sum += diff;
        count++;
    }

    Ping();
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

void LogLatencyThread()
{
    while (!cts.IsCancellationRequested)
    {
        if (count > 0)
            Console.WriteLine($"Ping: {sum / count / TimeSpan.TicksPerMillisecond:N0}");

        lock (sync)
        {
            count = 0;
            sum = 0;
        }

        Thread.Sleep(1000);
    }
}

void KeepAlive(CancellationTokenSource cts = null)
{
    cts ??= new CancellationTokenSource();

    Console.CancelKeyPress += (x, y) =>
    {
        cts.Cancel();
    };

    while (!cts.IsCancellationRequested)
    {
        Thread.Sleep(1000);
    }
}

void Ping()
{
    var time = new long[] { DateTime.Now.Ticks }.AsSpan();
    var timeBytes = MemoryMarshal.Cast<long, byte>(time);
    var bytesToSend = Utilities.AddLength(timeBytes.ToArray());
    _socket.Send(bytesToSend);
}