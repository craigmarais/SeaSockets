namespace CSockets
{
    public class CClientSocket
    {
        public delegate void ConnectionEstablished();
        public event ConnectionEstablished OnConnected;
        public event CSocket.MessageReceived OnMessageReceived;
        public event CSocket.ErrorOccured OnError;

        readonly CancellationToken _token;
        readonly CSocket _connection;

        public string Endpoint => _connection.Endpoint;
        public bool Connected => _connection.Connected;

        public CClientSocket(string host, ushort port, CancellationToken token)
        {
            _token = token;
            _connection = new CSocket(host, port, token);
        }

        public void Connect()
        {
            try 
            { 
                _connection.OnMessageReceived += OnMessageReceived;
                _connection.OnError += OnError;
                
                while (!_connection.Connected)
                    _connection.Connect();

                OnConnected?.Invoke();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.ToString(), _connection.Endpoint.ToString());
            }
        }

        public void Send(byte[] data)
        {
            try
            {
                _connection.Send(data);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.ToString(), _connection.Endpoint.ToString());
            }
        }
    }
}