using System.Net.Sockets;
using System.Net;
using System.Text;
using Protocolo;

namespace Cliente
{
    internal class ProgramCliente
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Inciar Cliente...");
            //1- Creamos un nuevo socket
            var socketCliente = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);
            //2- Creamos endpoint local con IP y puerto local
            var endpointLocal = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0);
            // 2.5 - Pongo en  0 el puerto para que se me asigne el primero disponible
            //3- Asociación entre el socket y el endpoint local
            socketCliente.Bind(endpointLocal);

            //4- Creamos endpoint remoto con IP y puerto del servidor
            var endpointServidor = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 20000);

            bool salirMenu = false;
            bool conectado = false;

            socketCliente.Connect(endpointServidor);
            conectado = true;

            ManejoDataSocket manejoDataSocket = new ManejoDataSocket(socketCliente);

            Console.Write("Ingrese su nombre de usuario: ");
            string usuario = Console.ReadLine();

            byte[] data = Encoding.UTF8.GetBytes(usuario);
            byte[] largoData = BitConverter.GetBytes(data.Length);

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

            Console.Write("Ingrese su contraseña: ");
            string contraseña = Console.ReadLine();

            data = Encoding.UTF8.GetBytes(contraseña);
            largoData = BitConverter.GetBytes(data.Length);

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

            while (!salirMenu)
            {
                Console.Clear();
                Console.WriteLine("Menú Principal");
   
                Console.WriteLine("1. Desconectarse");
                Console.WriteLine("2. Publicación de producto");
                Console.WriteLine("3. Compra de productos");
                Console.WriteLine("4. Modificación de producto");
                Console.WriteLine("5. Baja de producto");
                Console.WriteLine("6. Búsqueda de productos");
                Console.WriteLine("7. Consultar un producto específico.");
                Console.WriteLine("8. Calificar un producto");
                Console.WriteLine("9. Salir");
                Console.Write("Seleccione una opción: ");

                string opcion = Console.ReadLine();

                data = Encoding.UTF8.GetBytes(opcion);
                largoData = BitConverter.GetBytes(data.Length);
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

                switch (opcion)
                {
                    case "1":
                        

                        //5- Establecemos conexión del socket con el endpointServidor (remoto)
                        if (!conectado)
                        {
                            Console.WriteLine("Has seleccionado la opción Conectarse.");
                            

                        }
                        else
                        {
                            Console.WriteLine("Has seleccionado la opción Desconectarse.");
                            //socketCliente.Shutdown(SocketShutdown.Both);
                            conectado = false;
                            salirMenu = true;
                            //socketCliente.Close();
                        }                        
/*
                        Console.Write("Ingrese su contraseña: ");
                        string contraseña = Console.ReadLine();

                        data = Encoding.UTF8.GetBytes(contraseña);
                        largoData = BitConverter.GetBytes(data.Length);

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

                        bool ingresoExitoso = false;
                        try
                        {
                            // Recibo el largo del mensaje
                            largoData = manejoDataSocket.Receive(Constantes.LargoFijo);
                            // Recibo el mensaje
                            data = manejoDataSocket.Receive(BitConverter.ToInt32(largoData));

                            string respuestaAutenticacion = Encoding.UTF8.GetString(data);
                            if(respuestaAutenticacion == "true")
                            {
                                ingresoExitoso = true;
                                Console.WriteLine("Ingreso exitoso "+ usuario);
                            }
                            
                        }
                        catch (SocketException e)
                        {
                            break;
                        }

                        */

                        break;
                    case "2":
                        Console.WriteLine("Has seleccionado la opción Publicación de producto");

                        Console.Write("Nombre del producto: ");
                        string nombreProducto = Console.ReadLine();

                        data = Encoding.UTF8.GetBytes(nombreProducto);
                        largoData = BitConverter.GetBytes(data.Length);
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

                        Console.Write("Descripción del producto: ");
                        string descripcion = Console.ReadLine();

                        data = Encoding.UTF8.GetBytes(descripcion);
                        largoData = BitConverter.GetBytes(data.Length);
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

                        int cantidadDisponible;

                        while (true)
                        {
                            Console.Write("Cantidad disponible: ");
                            string input = Console.ReadLine();

                            if (int.TryParse(input, out cantidadDisponible))
                            {
                                break;
                            }
                            else
                            {
                                Console.WriteLine("Ingrese un número entero válido.");
                            }
                        }
                        string aux = "" + cantidadDisponible;
                        data = Encoding.UTF8.GetBytes(aux);
                        largoData = BitConverter.GetBytes(data.Length);
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


                        int precioProducto;
                        while (true)
                        {
                            Console.Write("Precio del  producto: ");
                            string input = Console.ReadLine();

                            if (int.TryParse(input, out precioProducto))
                            {
                                break;
                            }
                            else
                            {
                                Console.WriteLine("Ingrese un número válido.");
                            }
                        }

                        aux = "" + precioProducto;
                        data = Encoding.UTF8.GetBytes(aux);
                        largoData = BitConverter.GetBytes(data.Length);
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

                        Console.Write("Ruta de la imagen del producto: ");
                        string imagen = Console.ReadLine();

                        data = Encoding.UTF8.GetBytes(imagen);
                        largoData = BitConverter.GetBytes(data.Length);
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


                        break;
                    case "3":
                        Console.WriteLine("Has seleccionado la opción Compra de productos");
                        bool escucharProductos = true;
                        while(escucharProductos)
                        {
                            try
                            {
                                // Recibo el largo del mensaje
                                byte[] largoDataDelServidor = new byte[4];
                                int cantRecibida = socketCliente.Receive(largoData);

                                if (cantRecibida == 0)
                                {
                                    escucharProductos = false;
                                }
                                else
                                {
                                    int largo = BitConverter.ToInt32(largoData);

                                    // Recibo el mensaje
                                    data = new byte[largo];
                                    int recibidoData = socketCliente.Receive(data);
                                    if (recibidoData == 0)
                                    {
                                        conectado = false;
                                    }
                                    else
                                    {
                                        string mensaje = Encoding.UTF8.GetString(data);
                                        if (mensaje.Equals("end"))
                                        {
                                            escucharProductos = false;
                                        }
                                        else
                                        {
                                            Console.WriteLine("Producto: {0}", mensaje);
                                        }
                                        
                                    }
                                }
                            }
                            catch (SocketException e)
                            {
                                conectado = false;
                            }
                        }


                        break;
                    case "4":
                        Console.WriteLine("Has seleccionado la opción Modificación de producto");

                        break;
                    case "5":
                        Console.WriteLine("Has seleccionado la opción Baja de producto");
                        List<string> productNames = new List<string>();
                        bool escucharProductosAEliminar = true;
                        while (escucharProductosAEliminar)
                        {
                            try
                            {
                                byte[] largoDataDelServidor = new byte[4];
                                int cantRecibida = socketCliente.Receive(largoData);

                                if (cantRecibida == 0)
                                {
                                    escucharProductos = false;
                                }
                                else
                                {
                                    int largo = BitConverter.ToInt32(largoData);

                                    data = new byte[largo];
                                    int recibidoData = socketCliente.Receive(data);
                                    if (recibidoData == 0)
                                    {
                                        conectado = false;
                                    }
                                    else
                                    {
                                        string mensaje = Encoding.UTF8.GetString(data);
                                        if (mensaje.Equals("end"))
                                        {
                                            escucharProductosAEliminar = false;
                                        }
                                        else
                                        {
                                            Console.WriteLine("Producto: {0}", mensaje);
                                            productNames.Add(mensaje);
                                        }

                                    }
                                }
                            }
                            catch (SocketException e)
                            {
                                conectado = false;
                            }
                        }

                        Console.Write("Ingrese  nombre del producto a eliminar ");
                        string eleccion = Console.ReadLine();

                        data = Encoding.UTF8.GetBytes(eleccion);
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
                    case "6":
                        Console.WriteLine("Has seleccionado la opción Búsqueda de productos");

                        break;
                    case "7":
                        Console.WriteLine("Has seleccionado la opción Consultar un producto específico");

                        break;
                    case "8":
                        Console.WriteLine("Has seleccionado la opción Calificar un producto específico");

                        break;
                    case "9":
                        Console.WriteLine("Saliendo del programa...");
                        salirMenu = true;

                        break;
                    default:
                        Console.WriteLine("Opción no válida. Por favor, seleccione una opción válida.");

                        break;
                }

                Console.WriteLine("\nPresiona cualquier tecla para continuar...");
                Console.ReadKey();
            }

            socketCliente.Shutdown(SocketShutdown.Both);
            socketCliente.Close();

        }
    }
}

