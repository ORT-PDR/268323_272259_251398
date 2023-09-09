using Protocolo;
using Servidor;
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
            List<Producto> productos = new List<Producto>();
            List<Usuario> usuarios = new List<Usuario>();

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
                new Thread(() => HandleClient(socketClient, cantClientes, usuarios, productos)).Start();
            }

            //5- Aceptamos una conexión
           
            //Console.WriteLine("Nuevo Cliente conectado");
            //Console.ReadLine();
        }

        static void HandleClient(Socket socketClient, int nroCliente, List<Usuario> usuarios, List<Producto> productos) 
        {
            bool conectado = true;
            ManejoDataSocket manejoDataSocket = new ManejoDataSocket(socketClient);

            

            Usuario usuario = new Usuario();
            usuario.Id = nroCliente;
            byte[] largoData = manejoDataSocket.Receive(Constantes.LargoFijo);
            byte[] data = manejoDataSocket.Receive(BitConverter.ToInt32(largoData));
            try
            {
                usuario.Username = Encoding.UTF8.GetString(data);
                Console.WriteLine("nombre: " + usuario.Username);
            }
            catch (SocketException e)
            {
                conectado = false;
            }


            string contraseña = "";
            try
            {
                largoData = manejoDataSocket.Receive(Constantes.LargoFijo);
                data = manejoDataSocket.Receive(BitConverter.ToInt32(largoData));

                contraseña = Encoding.UTF8.GetString(data);
                Console.WriteLine("contraseña: " + contraseña);
                usuario.Password = contraseña;
            }
            catch (SocketException e)
            {
                conectado = false;
            }

            Console.WriteLine(usuario.Username+ " Conectado ");


            int opcion = 0;
           /* 
            try
            {
                largoData = manejoDataSocket.Receive(Constantes.LargoFijo);
                data = manejoDataSocket.Receive(BitConverter.ToInt32(largoData));
                string mensaje = Encoding.UTF8.GetString(data);
                opcion = int.Parse(mensaje);
            }
            catch (Exception ex)
            {
                
            }

            Console.WriteLine("seleccionada la opcion "+ opcion);
            */

                
            
            while (conectado)
            {
                try
                {
                    largoData = manejoDataSocket.Receive(Constantes.LargoFijo);
                    data = manejoDataSocket.Receive(BitConverter.ToInt32(largoData));
                    string mensaje = Encoding.UTF8.GetString(data);
                    opcion = int.Parse(mensaje);
                }
                catch (Exception ex)
                {

                }

                Console.WriteLine("seleccionada la opcion " + opcion);
                switch (opcion)
                {
                    case 1:

                        /*
                        if (usuarios.Contains(usuario))
                        {

                            data = Encoding.UTF8.GetBytes("true");
                            largoData = BitConverter.GetBytes(data.Length);
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

                        Console.WriteLine("Hola desde el hilo de " + usuario.Username);
                        */
                        break;
                    case 2:
                        Console.WriteLine("en case 2");
                        string nombreProducto = "";
                        try
                        {
                            largoData = manejoDataSocket.Receive(Constantes.LargoFijo);
                            data = manejoDataSocket.Receive(BitConverter.ToInt32(largoData));

                            nombreProducto = Encoding.UTF8.GetString(data);
                            Console.WriteLine("nombre producto: " + nombreProducto);
                        }
                        catch (SocketException e)
                        {
                            conectado = false;
                        }

                        string descripcion = "";
                        try
                        {
                            largoData = manejoDataSocket.Receive(Constantes.LargoFijo);
                            data = manejoDataSocket.Receive(BitConverter.ToInt32(largoData));

                            descripcion = Encoding.UTF8.GetString(data);
                            Console.WriteLine("descripcion producto " + descripcion);
                        }
                        catch (SocketException e)
                        {
                            conectado = false;
                        }

                        int stock = 0;
                        try
                        {
                            largoData = manejoDataSocket.Receive(Constantes.LargoFijo);
                            data = manejoDataSocket.Receive(BitConverter.ToInt32(largoData));
                            string aux = Encoding.UTF8.GetString(data);
                            stock = int.Parse(aux);
                            Console.WriteLine("stock producto " + stock);
                        }
                        catch (SocketException e)
                        {
                            conectado = false;
                        }

                        int precio = 0;
                        try
                        {
                            largoData = manejoDataSocket.Receive(Constantes.LargoFijo);
                            data = manejoDataSocket.Receive(BitConverter.ToInt32(largoData));
                            string aux = Encoding.UTF8.GetString(data);
                            precio = int.Parse(aux);
                            Console.WriteLine("precio producto " + precio);
                        }
                        catch (SocketException e)
                        {
                            conectado = false;
                        }

                        string imagen = "";
                        try
                        {
                            largoData = manejoDataSocket.Receive(Constantes.LargoFijo);
                            data = manejoDataSocket.Receive(BitConverter.ToInt32(largoData));

                            imagen = Encoding.UTF8.GetString(data);
                            Console.WriteLine("imagen producto " + imagen);
                        }
                        catch (SocketException e)
                        {
                            conectado = false;
                        }

                        Producto producto = new Producto();
                        producto.Name = nombreProducto;
                        producto.Description = descripcion;
                        producto.Price = precio;
                        producto.Stock = stock;
                        producto.Image = imagen;


                        if (!productos.Contains(producto))
                        {
                            productos.Add(producto);
                            Console.WriteLine(" se agrego el producto");
                        }
                        Console.WriteLine("esperando");
                        break;
                    case 3:
                        for(int i=0; i<productos.Count; i++)
                        {
                            string productoMostrado = productos.ElementAt(i).Name;
                            data = Encoding.UTF8.GetBytes(productoMostrado);
                            largoData = BitConverter.GetBytes(data.Length);
                            try
                            {
                                manejoDataSocket.Send(largoData);
                                manejoDataSocket.Send(data);
                            }
                            catch (SocketException)
                            {
                                Console.WriteLine("Error de conexión");
                                
                            }
                        }
                        data = Encoding.UTF8.GetBytes("end");
                        largoData = BitConverter.GetBytes(data.Length);
                        try
                        {
                            manejoDataSocket.Send(largoData);
                            manejoDataSocket.Send(data);
                        }
                        catch (SocketException)
                        {
                            Console.WriteLine("Error de conexión");

                        }

                        break;

                }
                /*
                try
                {
                    // Recibo el largo del mensaje
                    largoData = manejoDataSocket.Receive(Constantes.LargoFijo);
                    // Recibo el mensaje
                    data = manejoDataSocket.Receive(BitConverter.ToInt32(largoData));

                    string mensaje = $"El cliente dice {Encoding.UTF8.GetString(data)}";
                    Console.WriteLine(mensaje);
                }
                catch (SocketException e)
                {
                    conectado = false;
                }
                */

            }
            Console.WriteLine("Cliente {0} desconectado", nroCliente);

        }
    }
}