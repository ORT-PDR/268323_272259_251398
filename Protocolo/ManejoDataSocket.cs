using System.Net.Sockets;

namespace Protocolo
{
    public class ManejoDataSocket
    {
        private readonly Socket _socket;

        public ManejoDataSocket(Socket socket)
        {
            _socket = socket;
        }

        public void Send(byte[] data)
        {

        }
    }
}