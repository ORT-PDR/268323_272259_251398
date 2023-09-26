using System.Net.Sockets;
using System.Net;
using System.Text;
using Protocolo;
using Microsoft.VisualBasic.FileIO;
using Common;

namespace Cliente
{
    internal class ProgramCliente
    {
        static readonly SettingsManager settingMng = new SettingsManager();
        static void Main(string[] args)
        {
            Println("Inciar Cliente...");
            
            //CONECTAR AL SERVIDOR
            var socketClient = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);

            string clientIp = settingMng.ReadSettings(ClientConfig.clientIPconfigKey);
            int clientPort = int.Parse(settingMng.ReadSettings(ClientConfig.clientPortconfigKey));
            string serverIp = settingMng.ReadSettings(ClientConfig.serverIPconfigKey);
            int serverPort = int.Parse(settingMng.ReadSettings(ClientConfig.serverPortconfigKey));

            var localEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0);
            socketClient.Bind(localEndPoint);
            var remoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 20000);
            socketClient.Connect(remoteEndPoint);
            
            bool exitMenu = false;
            bool connected = true;

            SocketHelper socketHelper = new SocketHelper(socketClient);

            Println("Ingrese la opción:");
            Println("1. Iniciar Sesión");
            Println("2. Registrarse");
            string initOption = Console.ReadLine();
            while (initOption != "1" && initOption != "2")
            {
                Println("Ingrese una opcion valida.");
                initOption = Console.ReadLine();
            }
            SendData(socketHelper, initOption);
            if (initOption == "1") LogIn(socketHelper);
            else RegisterUser(socketHelper);

            while (!exitMenu)
            {
                ShowMenu();
                string option = Read();
                SendData(socketHelper, option);
                switch (option)
                {
                    case "1":
                        PublishProduct(socketHelper);

                        break;
                    case "2":
                        BuyAProduct(socketHelper, socketClient, ref connected);
                        
                        break;
                    case "3":
                        ModifyAProduct(ref connected, socketHelper);

                        break;
                    case "4":
                        DeleteProduct(ref connected, socketHelper);

                        break;
                    case "5":
                        SearchProductByFilter(ref connected, socketHelper);

                        break;
                    case "6":
                        ConsultAProduct(ref connected, socketHelper);

                        break;
                    case "7":
                        RateAProduct(socketHelper);

                        break;
                    case "8":
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

        private static void RegisterUser(SocketHelper socketHelper)
        {
            Print("Ingrese nombre de usuario:   ");
            string username = Console.ReadLine();
            Print("Ingrse constraseña:   ");
            string password = Console.ReadLine();
            while (password == "")
            {
                Print("La contraseña no puede ser vacía. Ingrese una contraseña nuevamente:  ");
                password = Console.ReadLine();
            }
            string user = username + "@" + password;
            SendData(socketHelper, user);
            Println("Bienvenido al sistema " + username);
        }

        private static void DeleteProduct(ref bool connected, SocketHelper socketHelper)
        {
            Println("Has seleccionado la opción Baja de producto");
            List<string> userProducts = GetUserProducts(socketHelper, ref connected);
            ShowProducts(userProducts);
            Console.Write("Ingrese el nombre del producto a eliminar");
            string prodName = Read();
            SendData(socketHelper, prodName);
            string response = "";
            ReceiveData(socketHelper, ref response);
            Println(response);
        }

        private static void SearchProductByFilter(ref bool connected, SocketHelper socketHelper)
        {
            Println("Has seleccionado la opción Búsqueda de productos");
            Print("Ingrese el nombre para filtrar: ");
            string filterText = Console.ReadLine();
            SendData(socketHelper, filterText);
            string product = "";
            while (product != "end")
            {
                Println(product);
                connected = ReceiveData(socketHelper, ref product);
                if (!connected) Println("No se pudo realizar la busqueda.");
            }
        }

        private static void ModifyAProduct(ref bool connected, SocketHelper socketHelper)
        {
            Println("Has seleccionado la opción Modificación de producto");
            List<string> productNames = GetUserProducts(socketHelper, ref connected);
            Print("Ingrese nombre del producto a modificar ");
            string product = Read();
            while (!productNames.Contains(product))
            {
                Print("Producto no encontrado. Escriba un nombre de producto válido.");
                product = Read();
            }

            bool modificado = false;
            while (!modificado)
            {
                PrintModifyProductOptions();
                string attributeOption = Read();
                while (attributeOption != "1" && attributeOption != "2" &&
                    attributeOption != "3" && attributeOption != "4")
                {
                    Println("Opcion Inválida.");
                    PrintModifyProductOptions();
                    attributeOption = Read();
                }
                SendData(socketHelper, product);
                SendData(socketHelper, attributeOption);

                Print("Inserte nuevo valor:");
                string newValue = Read();

                if (attributeOption == "2")
                {
                    int value = -1;
                    while (value < 0)
                    {
                        try
                        {
                            value = Convert.ToInt32(newValue);
                        }
                        catch
                        {
                            Console.WriteLine("El stock debe ser un numero mayor o igual a 0. Inserte nuevamente:");
                            newValue = Read();
                        }
                    }
                }
                else if (attributeOption == "3")
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
                            newValue = Read();
                        }
                    }
                }
                SendData(socketHelper, newValue);
                modificado = true;
            }
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
            Println("1. Publicación de producto");
            Println("2. Compra de productos");
            Println("3. Modificación de producto");
            Println("4. Baja de producto");
            Println("5. Búsqueda de productos");
            Println("6. Consultar un producto específico.");
            Println("7. Calificar un producto");
            Println("8. Salir");
            Println("");
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

        private static void SendData(SocketHelper socketHelper, string text)
        {
            byte[] data = Encoding.UTF8.GetBytes(text);
            byte[] dataLength = BitConverter.GetBytes(data.Length);

            try
            {
                socketHelper.Send(dataLength);
                socketHelper.Send(data);
            }
            catch (SocketException)
            {
                Println("Error de conexión");
            }
        }

        private static bool ReceiveData(SocketHelper socketHelper, ref string text)
        {
            try
            {
                byte[] largoData = socketHelper.Receive(Protocol.DataSize);
                byte[] data = socketHelper.Receive(BitConverter.ToInt32(largoData));

                text = Encoding.UTF8.GetString(data);
                return true;
            }
            catch (SocketException e)
            {
                return false;
            }
        }

        private static void PublishProduct(SocketHelper socketHelper)
        {
            Println("Has seleccionado la opción Publicación de producto");

            Print("Nombre del producto: ");
            string productName = Console.ReadLine();
            SendData(socketHelper, productName);
            string isOK = "";
            ReceiveData(socketHelper, ref isOK);
            while (isOK != "OK")
            {
                Print("El producto ya se encuentra en el sistema. Inserte un nombre de producto no publicado:  ");
                productName = Console.ReadLine();
                SendData(socketHelper, productName);
                ReceiveData(socketHelper, ref isOK);
            }

            Print("Descripción del producto: ");
            string productDescription = Console.ReadLine();
            SendData(socketHelper, productDescription);

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
            SendData(socketHelper, aux);

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
            SendData(socketHelper, aux);

            Print("Ruta de la imagen del producto: ");
            string image = Console.ReadLine();

            SendData(socketHelper, image);

            ReceiveData(socketHelper, ref isOK);
            if (isOK == "OK") Println("Se agregó el producto correctamente.");
        }

        private static void RateAProduct(SocketHelper socketHelper)
        {
            Println("Has seleccionado la opción Calificar un producto");

            Println("Ingrese el id del producto que desea calificar");
            var id = Console.ReadLine();
            SendData(socketHelper, id);

            Println("¿Cuál es su opinión del producto?");
            var opinion = Console.ReadLine();
            SendData(socketHelper, opinion);

            Println("Califique el producto");
            var rating = Console.ReadLine();
            SendData(socketHelper, rating);
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

        private static List<string> GetUserProducts(SocketHelper socketHelper, ref bool connected)
        {
            string strQuantProducts = "";
            connected = ReceiveData(socketHelper, ref strQuantProducts);
            int quantProducts = int.Parse(strQuantProducts);

            List<string> products = new List<string>();

            for (int i=0; i<quantProducts; i++)
            {
                string prod = "";
                connected = ReceiveData(socketHelper, ref prod);
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

        private static void ConsultAProduct(ref bool connected, SocketHelper socketHelper)
        {
            Println("Has seleccionado la opción Consultar un producto específico");
            Println("Productos disponibles:");
            List<string> productsToConsult = GetUserProducts(socketHelper, ref connected);
            ShowProducts(productsToConsult);
            Print("Ingrese el nombre del producto que quiera consultar ");
            string prodName = Read();
            while (!productsToConsult.Contains(prodName))
            {
                Print("Ingrese alguna de las opciones listadas: ");
                prodName = Read();
            }
            Println("Información sobre el producto: " + prodName);
            SendData(socketHelper, prodName);
            string consultedProduct = "";
            ReceiveData(socketHelper, ref consultedProduct);
            Println(consultedProduct);
        }

        private static void BuyAProduct(SocketHelper socketHelper, Socket socketClient, ref bool connected)
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
                                SendData(socketHelper, message);
                                connected = ReceiveData(socketHelper, ref message);
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

        private static void LogIn(SocketHelper socketHelper)
        {
            bool correctUser = false;
            while (!correctUser)
            {
                Print("Ingrese su nombre de usuario: ");
                string userName = Read();
                Print("Ingrese su contraseña: ");
                string userPass = Read();
                string user = userName + "#" + userPass;
                SendData(socketHelper, user);

                string response = "";
                ReceiveData(socketHelper, ref response);

                if (response == "ok")
                {
                    Println("Bienvenido al sistema " + userName);
                    correctUser = true;
                    socketHelper.UserName = userName;
                }
                else
                {
                    Println(response);
                }
            }
        }
    }
}

