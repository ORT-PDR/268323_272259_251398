using System.Net.Sockets;
using System.Net;
using System.Text;

namespace Cliente
{
    internal class ProgramCliente
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Inicializando Cliente...");
            //1- Creamos un nuevo socket
            var socketCliente = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);
            //2- Creamos endpoint local con IP y puerto local
            var endpointLocal = new IPEndPoint(IPAddress.Parse("172.17.10.1"), 0);
            // 2.5 - Pongo en  0 el puerto para que se me asigne el primero disponible
            //3- Asociación entre el socket y el endpoint local
            socketCliente.Bind(endpointLocal);

            //4- Creamos endpoint remoto con IP y puerto del servidor
            var endpointServidor = new IPEndPoint(IPAddress.Parse("172.17.10.1"), 20000);
            //5- Establecemos conexión del socket con el endpointServidor (remoto)
            socketCliente.Connect(endpointServidor);


            Console.WriteLine("Cliente conectado");
            Console.WriteLine("Escribir un mensaje al servidor:");
            var salir = false;

            while (!salir)
            {
                string mensaje = Console.ReadLine();

                if (mensaje.Equals("exit"))
                {
                    salir = true;
                }
                else
                {


                    byte[] data = Encoding.UTF8.GetBytes(mensaje);
                    socketCliente.Send(data);
                }
            }

            socketCliente.Shutdown(SocketShutdown.Both);
            socketCliente.Close();

        }
    }
}