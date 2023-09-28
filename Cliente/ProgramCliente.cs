using System.Net.Sockets;
using System.Net;
using System.Text;
using Protocolo;
using Microsoft.VisualBasic.FileIO;
using Common;
using System.IO;
using System;

namespace Cliente
{
    internal class ProgramCliente
    {
        static readonly SettingsManager settingMng = new SettingsManager();
        static bool errorDeConexion = true;
        static Socket socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        static string clientIp = settingMng.ReadSettings(ClientConfig.clientIPconfigKey);
        static int clientPort = int.Parse(settingMng.ReadSettings(ClientConfig.clientPortconfigKey));
        static string serverIp = settingMng.ReadSettings(ClientConfig.serverIPconfigKey);
        static int serverPort = int.Parse(settingMng.ReadSettings(ClientConfig.serverPortconfigKey));

        static IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0);
        //socketClient.Bind(localEndPoint);
        static IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 20000);

        static void Main(string[] args)
        {
            Println("Inciar Cliente...");
            
            //CONECTAR AL SERVIDOR
            //var socketClient = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);

            //string clientIp = settingMng.ReadSettings(ClientConfig.clientIPconfigKey);
            //int clientPort = int.Parse(settingMng.ReadSettings(ClientConfig.clientPortconfigKey));
            //string serverIp = settingMng.ReadSettings(ClientConfig.serverIPconfigKey);
            //int serverPort = int.Parse(settingMng.ReadSettings(ClientConfig.serverPortconfigKey));

            //var localEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0);
            socketClient.Bind(localEndPoint);
            //var remoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 20000);
            
            
            
            bool exitMenu = false;
            bool connected = true;
            

            SocketHelper socketHandler = new SocketHelper(socketClient);
          //  while(errorDeConexion)
            //{
                LogIn(socketHandler, socketClient, remoteEndPoint);
           // }
            


            while (!exitMenu && !errorDeConexion)
            {
                ShowMenu();
                string option = Read();
                try
                {
                    int checkOption = int.Parse(option);
                    SendData(socketHandler, option);
                }catch (Exception ex)
                {

                }
              
                switch (option)
                {
                    case "1":
                        if(!errorDeConexion)
                        {
                            PublishProduct(socketHandler, socketClient);
                        }
                        break;
                    case "2":
                        if (!errorDeConexion)
                        {
                            BuyAProduct(socketHandler, socketClient, ref connected);
                        }                                             
                        break;
                    case "3":
                        if(!errorDeConexion)
                        {
                            ModifyAProduct(ref connected, socketHandler, socketClient);
                        }                       
                        break;
                    case "4":
                        if (!errorDeConexion)
                        {
                            DeleteProduct(ref connected, socketHandler);
                        }
                        break;
                    case "5":
                        if (!errorDeConexion)
                        {
                            SearchProductByFilter(ref connected, socketHandler);
                        }
                        break;
                    case "6":
                        if (!errorDeConexion)
                        {
                            ConsultAProduct(ref connected, socketHandler, socketClient);
                        }
                        break;
                    case "7":
                        if (!errorDeConexion)
                        {
                            RateAProduct(socketHandler, ref  connected);
                        }
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

            try
            {
                socketClient.Shutdown(SocketShutdown.Both);
            }catch(Exception e)
            {
                Console.WriteLine("Servidor caido, quiere reintentar?");
                Console.WriteLine("1-Si");
                Console.WriteLine("2-No");
                bool eleccionValida = false;
                int eleccion = 0;
                while (!eleccionValida)
                {
                    try
                    {
                        eleccion = int.Parse(Read());
                        eleccionValida = true;
                    }
                    catch (Exception exception)
                    {
                        Println("Ingrese 1 o 2 según su eleccón ");
                    }
                }
                if(eleccion == 1)
                {
                    ReconectarAlServidor();
                    LogIn(socketHandler, socketClient, remoteEndPoint);
                }

                
            }
            
            socketClient.Close();

        }

        private static void DeleteProduct(ref bool connected, SocketHelper socketHelper)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
           
        }

        private static void SearchProductByFilter(ref bool connected, SocketHelper socketHelper)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine("Error de conexion");
            }
          
        }

        private static void ModifyAProduct(ref bool connected, SocketHelper socketHelper, Socket socketClient)
        {
            try
            {
                Println("Has seleccionado la opción Modificación de producto");
                List<string> productNames = GetUserProducts(socketHelper, ref connected);
                for (int i = 0; i < productNames.Count; i++)
                {
                    Println($"{productNames[i]}");
                }
                Println("Ingrese nombre del producto a modificar ");
                string product = Read();
                while (!productNames.Contains(product))
                {
                    Println("Producto no encontrado. Escriba un nombre de producto válido.");
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
                        SendData(socketHelper, newValue);
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
                        SendData(socketHelper, newValue);
                    }
                    else if (attributeOption == "4") //Modificar imagen
                    {
                        string imageName = product + "InServer.png";
                        var fileCommonHandler = new FileCommsHandler(socketClient);
                        try
                        {
                           
                            fileCommonHandler.SendFile(newValue, imageName);
                            SendData(socketHelper, newValue);
                            Console.WriteLine("Se envio el archivo nuevo al Servidor");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            SendData(socketHelper, "");
                        }
                    }
                    //SendData(socketHelper, newValue);
                    modificado = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error de conexion");
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
                errorDeConexion = true;
                throw new Exception("Error de conexion");
                
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

        private static void PublishProduct(SocketHelper socketHelper, Socket socketClient)
        {
            try
            {
                Println("Has seleccionado la opción Publicación de producto");
                SendData(socketHelper, "Creando Proucto");
                Print("Nombre del producto: ");
                string productName = Console.ReadLine();
                SendData(socketHelper, productName);

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
                //pasar la imagen al servidor
                Println("Desea agregar imagen?");
                Println("1-Si");
                Println("2-No");
                bool eleccionValida = false;
                int eleccion = 0;
                while (!eleccionValida)
                {                   
                    try
                    {
                        eleccion = int.Parse(Read());
                        eleccionValida = true;
                    }catch (Exception e)
                    {
                        Println("Ingrese 1 o 2 según su eleccón ");
                    }
                }

                if (eleccion == 1)
                {
                    SendData(socketHelper, eleccion+"");
                    Print("Ruta de la imagen del producto: ");
                    string path = Console.ReadLine();
                    string imageName = productName + "InServer.png";

                    var fileCommonHandler = new FileCommsHandler(socketClient);
                    try
                    {
                        fileCommonHandler.SendFile(path, imageName);
                        SendData(socketHelper, path);
                        Console.WriteLine("Se envio el archivo al Servidor");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        SendData(socketHelper, "");
                    }
                }
                else
                {
                    SendData(socketHelper, eleccion + "");
                    SendData(socketHelper, "sin imagen");
                }
               
                

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error de conexion");
                

            }

        }

        private static void ReconectarAlServidor()
        {
            socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            clientIp = settingMng.ReadSettings(ClientConfig.clientIPconfigKey);
            clientPort = int.Parse(settingMng.ReadSettings(ClientConfig.clientPortconfigKey));
            serverIp = settingMng.ReadSettings(ClientConfig.serverIPconfigKey);
            serverPort = int.Parse(settingMng.ReadSettings(ClientConfig.serverPortconfigKey));

            localEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0);
            socketClient.Bind(localEndPoint);
            remoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 20000);
        }
        private static void RateAProduct(SocketHelper socketHelper, ref bool connected)
        {
            try
            {
                Println("Has seleccionado la opción Calificar un producto");

                List<string> productsToRate = GetUserProducts(socketHelper, ref connected);
                ShowProducts(productsToRate);

                Console.Write("Ingrese el nombre del producto a calificar: ");
                string prodName = Read();
                while (!productsToRate.Contains(prodName))
                {
                    Print("Ingrese alguna de las opciones listadas: ");
                    prodName = Read();
                }
                SendData(socketHelper, prodName);

                Println("¿Cuál es su opinión del producto?");
                var opinion = Console.ReadLine();
                SendData(socketHelper, opinion);

                string input = "";
                while (true)
                {
                    /*
                    Println("Califique el producto con numero del 1 al 10");
                    input = Console.ReadLine();

                    if (int.Parse(input) >=1 && int.Parse(input)<=10)
                    {
                        break;
                    }
                    else
                    {
                        Println("Ingrese un número entero válido.");
                    }
                    */
                    Println("Califique el producto con numero del 1 al 10");
                    input = Read();
                    try
                    {
                        int checkOption = int.Parse(input);
                        if(checkOption >= 1 && checkOption <= 10)
                        {
                            SendData(socketHelper, input);
                            break;
                        }
                        
                    }
                    catch (Exception ex)
                    {
                        Println("Ingrese un número entero válido.");
                    }
                }

                
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error de conexion");
            }
            
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
                //try
                //{
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
                //}
                /*catch (SocketException e)
                {
                    connected = false;
                    escucharProductosAEliminar = false;

                }*/
            }
            return products;
        }

        private static void ConsultAProduct(ref bool connected, SocketHelper socketHelper, Socket socketClient)
        {
            try
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

                Console.WriteLine("Antes de recibir el archivo");
                var fileCommonHandler = new FileCommsHandler(socketClient);
                fileCommonHandler.ReceiveFile();
                string productImage = prodName;
                string imageName = productImage;
                connected = ReceiveData(socketHelper, ref productImage);
                Console.WriteLine("Archivo recibido!!");


               
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error de conexion");
            }
            
        }

        private static void BuyAProduct(SocketHelper socketHelper, Socket socketClient, ref bool connected)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine("Error de conexion");
            }
            
        }

        private static void LogIn(SocketHelper socketHelper, Socket socketClient, IPEndPoint remoteEndPoint)
        {
            try
            {
                bool usuarioIncorrecto = false;
                bool correctUser = false;
                while (!correctUser)
                {
                    Print("Ingrese su nombre de usuario: ");
                    string userName = Read();
                    Print("Ingrese su contraseña: ");
                    string userPass = Read();
                    string user = userName + "#" + userPass;

                    if (!usuarioIncorrecto)
                    {
                        socketClient.Connect(remoteEndPoint);
                    }
                    SendData(socketHelper, user);

                    string response = "";
                    ReceiveData(socketHelper, ref response);

                    if (response == "ok")
                    {
                        Println("Bienvenido al sistema " + userName);
                        correctUser = true;
                        errorDeConexion = false;
                    }
                    else
                    {
                        Println(response);
                        usuarioIncorrecto |= true;

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("No fue posible acceder al servidor");
                errorDeConexion = true;
            }
            
        }
    }
}

