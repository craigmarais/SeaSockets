using ClientSocket;

CancellationTokenSource cts = new CancellationTokenSource();
var connections = new List<AckClientConnection>();

new Thread(LogLatencyThread).Start();

if (!int.TryParse(args[0], out var clientCount) || clientCount <= 0)
{
    Console.WriteLine($"Argument 0 is not a valid number");
}

for (int i = 0; i < clientCount; i++)
{
    connections.Add(new AckClientConnection("127.0.0.1", 7324, cts.Token));
    Console.WriteLine($"Added connection {i}");
}

for (int i = 0; i < clientCount; i++)
{
    connections[i].Start();
    Console.WriteLine($"Added connection {i}");
}

KeepAlive(cts);

void LogLatencyThread()
{
    while (!cts.IsCancellationRequested)
    {
        int count = 0;
        long sum = 0;
        for (int i = 0; i < connections.Count; i++)
        {
            var connection = connections[i];
            var metric = connection.ReadMetrics();
            count += metric.Item1;
            sum += metric.Item2;
        }

        if (count != 0)
            Console.WriteLine($"Connection Ping: {sum / count / TimeSpan.TicksPerMillisecond:N0} ms | Throughput: {count:N0}");

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
