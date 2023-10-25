using System.Net.Sockets;

namespace Protocolo
{
    public class SocketHelper
    {
        private readonly TcpClient _client;
        private string _userName;

        public string UserName { get => _userName; set => _userName = value; }

        public SocketHelper(TcpClient client)
        {
            _client = client;
        }

        public async Task SendAsync(byte[] data) 
        {
            try
            {
                var networkStream = _client.GetStream();
                await networkStream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception)
            {
                throw new SocketException();
            }
        }

        public async Task <byte[]> ReceiveAsync(int size) 
        {
            byte[] data = new byte[size];
            int offset = 0;
            var networkStream = _client.GetStream();

            while (offset < size)
            {
                try
                {
                    int received = await networkStream.ReadAsync(data, offset, size - offset);
                    
                    if (received == 0)
                    {
                        throw new SocketException();
                    }
                    offset += received;

                }
                catch (Exception)
                {
                    throw new SocketException();
                }
                
            }
            return data;
        }
    }
}
