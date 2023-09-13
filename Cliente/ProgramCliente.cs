using System.Net.Sockets;
using System.Net;
using System.Text;
using Protocolo;
using Microsoft.VisualBasic.FileIO;

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
            socketClient.Connect(endpointServer);
            
            bool exitMenu = false;
            bool connected = true;

            ManejoDataSocket manejoDataSocket = new ManejoDataSocket(socketClient);

            Print("Ingrese su nombre de usuario: ");
            string user = Read();
            SendData(manejoDataSocket, user);

            Print("Ingrese su contraseña: ");
            string pswd = Read();
            SendData(manejoDataSocket, pswd);

            while (!exitMenu)
            {
                ShowMenu();
                string option = Read();
                SendData(manejoDataSocket, option);
                switch (option)
                {
                    case "1":

                        break;
                    case "2":
                        PublishProduct(manejoDataSocket);

                        break;
                    case "3":
                        BuyAProduct(manejoDataSocket, socketClient, ref connected);
                        
                        break;

                    case "4":
                        Println("Has seleccionado la opción Modificación de producto");
                        List<string> productNames = GetUserProducts(manejoDataSocket, ref connected);
                        Console.Write("Ingrese nombre del producto a modificar ");
                        string product = Console.ReadLine();
                        while (!productNames.Contains(product))
                        {
                            Console.Write("Producto no encontrado. Escriba un nombre de producto válido.");
                            product = Console.ReadLine();
                        }
                        
                        bool modificado = false;
                        while (!modificado)
                        {
                            PrintModifyProductOptions();
                            string attributeOption = Console.ReadLine();
                            while (attributeOption != "1" && attributeOption != "2" &&
                                attributeOption != "3" && attributeOption != "4")
                            {
                                Console.WriteLine("Opcion Inválida.");
                                PrintModifyProductOptions();
                                attributeOption = Console.ReadLine();
                            }
                            SendData(manejoDataSocket, product);
                            SendData(manejoDataSocket, attributeOption);

                            Console.Write("Inserte nuevo valor:");
                            string newValue = Console.ReadLine();

                            if (attributeOption == "2")
                            {
                                int value = -1;
                                while(value < 0)
                                {
                                    try
                                    {
                                        value = Convert.ToInt32(newValue);
                                    }
                                    catch
                                    {
                                        Console.WriteLine("El stock debe ser un numero mayor o igual a 0. Inserte nuevamente:");
                                        newValue = Console.ReadLine();
                                    }
                                }
                            }

                            if (attributeOption == "3")
                            {
                                int value = -1;
                                while (value <= 0)
                                {
                                    try
                                    {
                                        value = Convert.ToInt32(newValue);
                                    }
                                    catch
                                    {
                                        Console.WriteLine("El precio debe ser un numero positivo. Inserte nuevamente:");
                                        newValue = Console.ReadLine();
                                    }
                                }

                            }

                            SendData(manejoDataSocket, newValue);
                            modificado = true;
                        }
                        break;
                    case "5":
                        Println("Has seleccionado la opción Baja de producto");
                        List<string> userProducts = GetUserProducts(manejoDataSocket, ref connected);
                        ShowProducts(userProducts);
                        Console.Write("Ingrese el nombre del producto a eliminar ");
                        string prodName = Read();
                        SendData(manejoDataSocket, prodName);
                        string response = "";
                        ReceiveData(manejoDataSocket, ref response);
                        Println(response);
                        break;
                    case "6":
                        Println("Has seleccionado la opción Búsqueda de productos");
                        Print("Ingrese el nombre para filtrar: ");
                        string filterText = Console.ReadLine();
                        SendData(manejoDataSocket, filterText);
                        product = "";
                        while (product != "end")
                        {
                            Println(product);
                            ReceiveData(manejoDataSocket, ref product);
                        }

                        break;
                    case "7":
                        ConsultAProduct(manejoDataSocket, ref connected);

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

        private static void PrintModifyProductOptions()
        {
            Console.WriteLine("Que campo desea modificar: (Digite la opción)");
            Console.WriteLine("1. Descripcion");
            Console.WriteLine("2. Stock Disponible");
            Console.WriteLine("3. Precio");
            Console.WriteLine("4. Imagen");
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

        private static string Read()
        {
            return Console.ReadLine();
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

        private static void ShowProducts(List<string> products)
        {
            Println("");
            foreach (string product in products)
            {
                Println(product);
            }
            Println("");
        }

        private static List<string> GetUserProducts(ManejoDataSocket manejoDataSocket, ref bool connected)
        {
            string strQuantProducts = "";
            connected = ReceiveData(manejoDataSocket, ref strQuantProducts);
            int quantProducts = int.Parse(strQuantProducts);

            List<string> products = new List<string>();

            for (int i=0; i<quantProducts; i++)
            {
                string prod = "";
                connected = ReceiveData(manejoDataSocket, ref prod);
                products.Add(prod);
            }

            return products;
        }

        private static List<NameStock> GetProductsToBuy(Socket socketClient, ref bool connected)
        {
            var products = new List<NameStock>();
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
                                NameStock product = new NameStock()
                                {
                                    Name = mensaje.Split("@")[0],
                                    Stock = mensaje.Split("@")[1]
                                };
                                products.Add(product);
                                Console.WriteLine("Producto: {0}   |   Stock: {1}", product.Name, product.Stock);
                            }
                        }
                    }
                }
                catch (SocketException e)
                {
                    connected = false;
                }
            }
            return products;
        }

        private static void ConsultAProduct(ManejoDataSocket manejoDataSocket, ref bool connected)
        {
            Println("Has seleccionado la opción Consultar un producto específico");
            Println("Productos disponibles:");
            List<string> productsToConsult = GetUserProducts(manejoDataSocket, ref connected);
            ShowProducts(productsToConsult);
            Print("Ingrese el nombre del producto que quiera consultar ");
            string prodName = Read();
            while (!productsToConsult.Contains(prodName))
            {
                Print("Ingrese alguna de las opciones listadas: ");
                prodName = Read();
            }
            Println("Información sobre el producto: " + prodName);
            SendData(manejoDataSocket, prodName);
            string consultedProduct = "";
            ReceiveData(manejoDataSocket, ref consultedProduct);
            Println(consultedProduct);
        }

        private static void BuyAProduct(ManejoDataSocket manejoDataSocket, Socket socketClient, ref bool connected)
        {
            Println("Has seleccionado la opción Compra de productos");
            bool isBought = false;
            while (!isBought)
            {
                List<NameStock> products = GetProductsToBuy(socketClient, ref connected);
                Print("Ingrese nombre del producto a comprar: ");
                string productToBuyName = Read();
                NameStock productNameStock = products.Find(p => p.Name.Equals(productToBuyName));
                if (productNameStock != null)
                {
                    int stock = int.Parse(productNameStock.Stock);
                    if (stock > 0)
                    {
                        Print("Ingrese cantidad a comprar: ");
                        int amountToBuy = -1;
                        while (amountToBuy < 1)
                        {
                            try
                            {
                                amountToBuy = int.Parse(Read());
                            }
                            catch
                            {
                                Print("Ingrese un numero positivo de cantidad a comprar: ");
                            }
                            if (amountToBuy > stock)
                            {
                                Println("Stock Insuficiente, inserte una cantidad que no exceda el stock:");
                                amountToBuy = -1;
                            }
                            else
                            {
                                string message = productNameStock.Name + "@" + amountToBuy.ToString();
                                SendData(manejoDataSocket, message);
                                connected = ReceiveData(manejoDataSocket, ref message);
                                if (connected)
                                {
                                    if (message.Equals("ok")) isBought = true;
                                    else Println("No hay stock disponible del producto seleccionado");
                                }
                            }
                        }
                    }
                    else
                    {
                        Println("No hay Stock del producto");
                    }
                }
                else
                {
                    Println("Nombre de producto no valido");
                }
            }
        }
    }
}

