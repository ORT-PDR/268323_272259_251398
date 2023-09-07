using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace Servidor
{
    internal class ProgramServidor
    {


        static void Main(string[] args)
        {
            Console.WriteLine("Configurando Servidor...");
            inicializarUsuarios();

            //1- Creamos un nuevo socket
            var socketServidor = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);

            //2- Creamos endpoint local con IP y puerto local
            var endpointLocal = new IPEndPoint(IPAddress.Parse("172.17.10.1"), 20000);

            //3- Asociación entre el socket y el endpoint
            socketServidor.Bind(endpointLocal);

            Console.WriteLine("Servidor Iniciado!");
            //4- Ponemos el socket en modo escucha
            socketServidor.Listen(10);
            Console.WriteLine("Esperando clientes....");
            int cantClientes = 0;
            while (true)
            {
                Socket socketClient = socketServidor.Accept(); //Bloqueante
                // Espera hasta que llega un nuevo cliente
                Console.WriteLine("Se conecto un cliente..");
                cantClientes++;
                // Lanzo un hilo para que lo maneje
                new Thread(() => HandleClient(socketClient, cantClientes)).Start();
            }

            //5- Aceptamos una conexión

            //Console.WriteLine("Nuevo Cliente conectado");
            //Console.ReadLine();
        }

        private static void inicializarUsuarios()
        {
            
        }

        static void HandleClient(Socket socketClient, int nroCliente)
        {
            bool conectado = true;

            Console.WriteLine("Hola desde el hilo del cliente " + nroCliente.ToString());
            while (conectado)
            {
                //byte[] data = new byte[8];
                try
                {
                    // Recibo el largo del mensaje
                    byte[] largoData = new byte[4];
                    int cantRecibida = socketClient.Receive(largoData);

                    if (cantRecibida == 0)
                    {
                        conectado = false;
                    }
                    else
                    {
                        int largo = BitConverter.ToInt32(largoData);
                        Console.WriteLine("El largo del mensaje es: {0}", largo);

                        // Recibo el mensaje
                        byte[] data = new byte[largo];
                        int recibidoData = socketClient.Receive(data);
                        if (recibidoData == 0)
                        {
                            conectado = false;
                        }
                        else
                        {
                            string mensaje = Encoding.UTF8.GetString(data);
                            Console.WriteLine("El cliente {0} dice: {1}", nroCliente, mensaje);
                        }
                    }
                }
                catch (SocketException e)
                {
                    conectado = false;
                }

            }
            Console.WriteLine("Cliente {0} desconectado", nroCliente);

        }
    }
}