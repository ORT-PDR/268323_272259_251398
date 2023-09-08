using Protocolo;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace PrimerEjemploSocket
{
    internal class ProgramServidor
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Inciar Servidor...");
            //1- Creamos un nuevo socket
            var socketServidor = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);
            //2- Creamos endpoint local con IP y puerto local
            var endpointLocal = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 20000);
            //3- Asociación entre el socket y el endpoint
            socketServidor.Bind(endpointLocal);
            //4- Ponemos el socket en modo escucha
            socketServidor.Listen(10);
            Console.WriteLine("Esperando por clientes....");
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

        static void HandleClient(Socket socketClient, int nroCliente) 
        {
            bool conectado = true;
            ManejoDataSocket manejoDataSocket = new ManejoDataSocket(socketClient);
            
            string nombre = "";
            try
            {
                // Recibo el largo del mensaje
                byte[] largoData = manejoDataSocket.Receive(Constantes.LargoFijo);
                // Recibo el mensaje
                byte[] data = manejoDataSocket.Receive(BitConverter.ToInt32(largoData));

                nombre =  Encoding.UTF8.GetString(data);
                Console.WriteLine("nombre: "+nombre);
            }
            catch (SocketException e)
            {
                conectado = false;
            }
            /*
            string contraseña = "";
            try
            {
                // Recibo el largo del mensaje
                byte[] largoData = manejoDataSocket.Receive(Constantes.LargoFijo);
                // Recibo el mensaje
                byte[] data = manejoDataSocket.Receive(BitConverter.ToInt32(largoData));

                nombre = Encoding.UTF8.GetString(data);
                Console.WriteLine("contraseña "+contraseña);
            }
            catch (SocketException e)
            {
                conectado = false;
            }

            if(nombre == "juan" && contraseña == "123")
            {

                byte[] data = Encoding.UTF8.GetBytes("true");
                byte[] largoData = BitConverter.GetBytes(data.Length);
                Console.WriteLine("conectado");
                try
                {
                    manejoDataSocket.Send(largoData); //Parte fija del mensaje. Largo
                    manejoDataSocket.Send(data); //Parte variable, el mensaje.
                }
                catch (SocketException)
                {
                    Console.WriteLine("Error de conexión");
                    //salir = true;
                }
            }
            */

            Console.WriteLine("Hola desde el hilo de " + nombre);
            while (conectado)
            {
                try
                {
                    // Recibo el largo del mensaje
                    byte[] largoData = manejoDataSocket.Receive(Constantes.LargoFijo);
                    // Recibo el mensaje
                    byte[] data = manejoDataSocket.Receive(BitConverter.ToInt32(largoData));

                    string mensaje = $"El cliente dice {Encoding.UTF8.GetString(data)}";
                    Console.WriteLine(mensaje);
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