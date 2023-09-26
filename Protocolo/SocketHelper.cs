using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Protocolo
{
    public class SocketHelper
    {
        private readonly Socket _socket;

        public SocketHelper(Socket socket)
        {
            _socket = socket;
        }

        public void Send(byte[] data) 
        {
            int offset = 0;
            int size = data.Length;
            while (offset < size)
            {
                try
                {
                    int cantEnviada = _socket.Send(data, offset, size - offset, SocketFlags.None);
                    if (cantEnviada == 0)
                    {
                        throw new SocketException();
                    }
                    offset += cantEnviada;
                    
                }catch (System.ObjectDisposedException ex)
                {
                    throw new SocketException();

                }
                
            }
        }

        public byte[] Receive(int size) 
        {
            byte[] data = new byte[size];
            int offset = 0;
            while (offset < size)
            {
                try
                {
                    int cantRecibido = _socket.Receive(data, offset, size - offset, SocketFlags.None);
                    if (cantRecibido == 0)
                    {
                        throw new SocketException();
                    }
                    offset += cantRecibido;

                }catch(System.ObjectDisposedException ex)
                {
                    throw new SocketException();
                }
                
            }
            return data;
        }
    }
}
