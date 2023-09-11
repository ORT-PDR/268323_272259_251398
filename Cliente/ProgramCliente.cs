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
            Println("Inciar Cliente...");
            
            //CONECTAR AL SERVIDOR
            var socketClient = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
            var endpointLocal = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0);
            socketClient.Bind(endpointLocal);
            var endpointServer = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 20000);
            //Acá no debería tirar una excepción si no se conecta?
            socketClient.Connect(endpointServer);
            
            bool exitMenu = false;
            bool connected = true;

            ManejoDataSocket manejoDataSocket = new ManejoDataSocket(socketClient);

            Print("Ingrese su nombre de usuario: ");
            string user = Console.ReadLine();
            SendData(manejoDataSocket, user);

            Print("Ingrese su contraseña: ");
            string pswd = Console.ReadLine();
            SendData(manejoDataSocket, pswd);

            while (!exitMenu)
            {
                ShowMenu();

                string option = Console.ReadLine();

                SendData(manejoDataSocket, option);

                switch (option)
                {
                    case "1":
                        if (!connected)
                        {
                            Println("Has seleccionado la opción Conectarse.");

                        }
                        else
                        {
                            Println("Has seleccionado la opción Desconectarse.");
                            //socketCliente.Shutdown(SocketShutdown.Both);
                            connected = false;
                            exitMenu = true;
                            //socketCliente.Close();
                        }                        

                        break;
                    case "2":
                        PublishProduct(manejoDataSocket);

                        break;
                    case "3":
                        Println("Has seleccionado la opción Compra de productos");

                        break;
                    case "4":
                        Println("Has seleccionado la opción Modificación de producto");

                        break;
                    case "5":
                        Console.WriteLine("Has seleccionado la opción Baja de producto");
                        List<string> productNames = new List<string>();
                        bool escucharProductosAEliminar = true;
                        byte[] data;
                        byte[] largoDataDelServidor;
                        while (escucharProductosAEliminar)
                        {
                            try
                            {
                                largoDataDelServidor = new byte[4];
                                int cantRecibida = socketClient.Receive(largoDataDelServidor);

                                if (cantRecibida == 0)
                                {
                                    escucharProductosAEliminar = false;
                                }
                                else
                                {
                                    int largo = BitConverter.ToInt32(largoDataDelServidor);

                                    data = new byte[largo];
                                    int recibidoData = socketClient.Receive(data);
                                    if (recibidoData == 0)
                                    {
                                        connected = false;
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
                                connected = false;
                            }
                        }

                        Console.Write("Ingrese  nombre del producto a eliminar ");
                        string eleccion = Console.ReadLine();

                        SendData(manejoDataSocket, eleccion);

                        break;
                    case "6":
                        Println("Has seleccionado la opción Búsqueda de productos");
                        Print("Ingrese el nombre para filtrar: ");
                        string filterText = Console.ReadLine();
                        SendData(manejoDataSocket, filterText);
                        string product = "";
                        while (product != "end")
                        {
                            Println(product);
                            ReceiveData(manejoDataSocket, ref product);
                        }

                        break;
                    case "7":
                        Console.WriteLine("Has seleccionado la opción Consultar un producto específico");

                        List<string> productosAConsultar = new List<string>();
                        bool escucharProductosAConsultar = true;
                        while (escucharProductosAConsultar)
                        {
                            try
                            {
                                largoDataDelServidor = new byte[4];
                                int cantRecibida = socketClient.Receive(largoDataDelServidor);

                                if (cantRecibida == 0)
                                {
                                    escucharProductosAConsultar = false;
                                }
                                else
                                {
                                    int largo = BitConverter.ToInt32(largoDataDelServidor);

                                    data = new byte[largo];
                                    int recibidoData = socketClient.Receive(data);
                                    if (recibidoData == 0)
                                    {
                                        connected = false;
                                    }
                                    else
                                    {
                                        string mensaje = Encoding.UTF8.GetString(data);
                                        if (mensaje.Equals("end"))
                                        {
                                            escucharProductosAConsultar = false;
                                        }
                                        else
                                        {
                                            Console.WriteLine("Producto: {0}", mensaje);
                                            productosAConsultar.Add(mensaje);
                                        }

                                    }
                                }
                            }
                            catch (SocketException e)
                            {
                                connected = false;
                            }
                        }

                        Console.Write("Ingrese  nombre del producto que quiera consultar ");
                        eleccion = Console.ReadLine();

                        while (!productosAConsultar.Contains(eleccion))
                        {
                            Console.Write("Ingrese alguna de las opciones listadas ");
                            eleccion = Console.ReadLine();
                        }


                        data = Encoding.UTF8.GetBytes(eleccion);
                        largoDataDelServidor = BitConverter.GetBytes(data.Length);

                        Console.WriteLine("Sobre el producto: " + eleccion);

                        try
                        {
                            manejoDataSocket.Send(largoDataDelServidor);
                            manejoDataSocket.Send(data);
                        }
                        catch (SocketException)
                        {
                            Console.WriteLine("Error de conexión");
                        }

                        try
                        {
                            largoDataDelServidor = new byte[4];
                            int cantRecibida = socketClient.Receive(largoDataDelServidor);

                            if (cantRecibida == 0)
                            {
                                escucharProductosAConsultar = false;
                            }
                            else
                            {
                                int largo = BitConverter.ToInt32(largoDataDelServidor);

                                data = new byte[largo];
                                int recibidoData = socketClient.Receive(data);
                                if (recibidoData == 0)
                                {
                                    connected = false;
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
                                        Console.WriteLine("stock: {0}", mensaje);
                                    }

                                }
                            }
                        }
                        catch (SocketException e)
                        {
                            connected = false;
                        }

                        try
                        {
                            largoDataDelServidor = new byte[4];
                            int cantRecibida = socketClient.Receive(largoDataDelServidor);

                            if (cantRecibida == 0)
                            {
                                escucharProductosAConsultar = false;
                            }
                            else
                            {
                                int largo = BitConverter.ToInt32(largoDataDelServidor);

                                data = new byte[largo];
                                int recibidoData = socketClient.Receive(data);
                                if (recibidoData == 0)
                                {
                                    connected = false;
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
                                        Console.WriteLine("descripcion: {0}", mensaje);
                                    }

                                }
                            }
                        }
                        catch (SocketException e)
                        {
                            connected = false;
                        }

                        try
                        {
                            largoDataDelServidor = new byte[4];
                            int cantRecibida = socketClient.Receive(largoDataDelServidor);

                            if (cantRecibida == 0)
                            {
                                escucharProductosAConsultar = false;
                            }
                            else
                            {
                                int largo = BitConverter.ToInt32(largoDataDelServidor);

                                data = new byte[largo];
                                int recibidoData = socketClient.Receive(data);
                                if (recibidoData == 0)
                                {
                                    connected = false;
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
                                        Console.WriteLine("precio: {0}", mensaje);
                                    }

                                }
                            }
                        }
                        catch (SocketException e)
                        {
                            connected = false;
                        }

                        break;
                    case "8":

                        RateAProduct(manejoDataSocket);

                        break;
                    case "9":
                        Println("Saliendo del programa...");
                        exitMenu = true;

                        break;
                    default:
                        Println("Opción no válida. Por favor, seleccione una opción válida.");

                        break;
                }

                Println("\nPresiona cualquier tecla para continuar...");
                Console.ReadKey();
            }

            socketClient.Shutdown(SocketShutdown.Both);
            socketClient.Close();

        }

        private static void ShowMenu()
        {
            Console.Clear();
            Println("Menú Principal");
            Println("1. Desconectarse");
            Println("2. Publicación de producto");
            Println("3. Compra de productos");
            Println("4. Modificación de producto");
            Println("5. Baja de producto");
            Println("6. Búsqueda de productos");
            Println("7. Consultar un producto específico.");
            Println("8. Calificar un producto");
            Println("9. Salir");
            Print("Seleccione una opción:");
        }

        private static void Print(string text)
        {
            Console.Write(text);
        }

        private static void Println(string text)
        {
            Console.WriteLine(text);
        }

        private static void SendData(ManejoDataSocket manejoDataSocket, string text)
        {
            byte[] data = Encoding.UTF8.GetBytes(text);
            byte[] dataLength = BitConverter.GetBytes(data.Length);

            try
            {
                manejoDataSocket.Send(dataLength);
                manejoDataSocket.Send(data);
            }
            catch (SocketException)
            {
                Println("Error de conexión");
            }
        }

        private static bool ReceiveData(ManejoDataSocket manejoDataSocket, ref string text)
        {
            try
            {
                byte[] largoData = manejoDataSocket.Receive(Constantes.LargoFijo);
                byte[] data = manejoDataSocket.Receive(BitConverter.ToInt32(largoData));

                text = Encoding.UTF8.GetString(data);
                return true;
            }
            catch (SocketException e)
            {
                return false;
            }
        }

        private static void PublishProduct(ManejoDataSocket manejoDataSocket)
        {
            Println("Has seleccionado la opción Publicación de producto");

            Print("Nombre del producto: ");
            string productName = Console.ReadLine();
            SendData(manejoDataSocket, productName);

            Print("Descripción del producto: ");
            string productDescription = Console.ReadLine();
            SendData(manejoDataSocket, productDescription);

            int stock;

            while (true)
            {
                Print("Cantidad disponible: ");
                string input = Console.ReadLine();

                if (int.TryParse(input, out stock))
                {
                    break;
                }
                else
                {
                    Println("Ingrese un número entero válido.");
                }
            }
            string aux = "" + stock;
            SendData(manejoDataSocket, aux);

            int precioProducto;
            while (true)
            {
                Print("Precio del  producto: ");
                string input = Console.ReadLine();

                if (int.TryParse(input, out precioProducto))
                {
                    break;
                }
                else
                {
                    Println("Ingrese un número válido.");
                }
            }

            aux = "" + precioProducto;
            SendData(manejoDataSocket, aux);

            Print("Ruta de la imagen del producto: ");
            string image = Console.ReadLine();

            SendData(manejoDataSocket, image);
        }


        private static void RateAProduct(ManejoDataSocket manejoDataSocket)
        {
            Println("Has seleccionado la opción Calificar un producto");

            Println("Ingrese el id del producto que desea calificar");
            var id = Console.ReadLine();
            SendData(manejoDataSocket, id);

            Println("¿Cuál es su opinión del producto?");
            var opinion = Console.ReadLine();
            SendData(manejoDataSocket, opinion);

            Println("Califique el producto");
            var rating = Console.ReadLine();
            SendData(manejoDataSocket, rating);
        }
    }
}

