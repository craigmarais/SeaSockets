using CSockets;
using System.Runtime.InteropServices;

CancellationTokenSource cts = new CancellationTokenSource();
var sync = new object();
int count = 0;
long sum = 0;

new Thread(LogLatencyThread).Start();

CServerSocket _socket = new CServerSocket(7324, cts.Token);
_socket.OnNewConnection += HandleNewConnection;
_socket.OnMessageReceived += HandlePing;
_socket.OnError += HandleError;
_socket.Listen();

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
    Ping(endpoint);
}

void HandleNewConnection(string endpoint)
{
    Console.WriteLine($"A new connection was made at {endpoint}");
}

void HandleError(string error, string endpoint)
{
    string connectionState = _socket.Connected(endpoint) ? "maintained" : "Broken";
    Console.WriteLine($"Error on {endpoint} connection. Remote connection is {connectionState}. Error: {error}");
}

void LogLatencyThread()
{
    while (!cts.IsCancellationRequested)
    {
        if (count > 0)
            Console.WriteLine($"Ping: {(sum / count) / TimeSpan.TicksPerMillisecond:N0} ms");

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

void Ping(string endpoint)
{
    var time = new long[] { DateTime.Now.Ticks }.AsSpan();
    var timeBytes = MemoryMarshal.Cast<long, byte>(time);
    var bytesToSend = Utilities.AddLength(timeBytes.ToArray());
    _socket.Send(bytesToSend, endpoint);
}