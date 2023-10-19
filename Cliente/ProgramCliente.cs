using System.Net.Sockets;
using System.Net;
using System.Text;
using Protocolo;
using Common;

namespace Cliente
{
    internal class ProgramCliente
    {
        static readonly SettingsManager settingMng = new();
        static bool conectionError = true;
        static string clientIp = settingMng.ReadSettings(ClientConfig.clientIPconfigKey);
        static int clientPort = int.Parse(settingMng.ReadSettings(ClientConfig.clientPortconfigKey));
        static string serverIp = settingMng.ReadSettings(ClientConfig.serverIPconfigKey);
        static int serverPort = int.Parse(settingMng.ReadSettings(ClientConfig.serverPortconfigKey));
        static bool exitMenu = false;
        static bool connected = true;

        static IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(clientIp), clientPort);
        static IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);

        static async Task Main(string[] args)
        {
            bool intentoConcetarme = true;
            while (intentoConcetarme)
            {
                clientIp = settingMng.ReadSettings(ClientConfig.clientIPconfigKey);
                clientPort = int.Parse(settingMng.ReadSettings(ClientConfig.clientPortconfigKey));
                serverIp = settingMng.ReadSettings(ClientConfig.serverIPconfigKey);
                serverPort = int.Parse(settingMng.ReadSettings(ClientConfig.serverPortconfigKey));

                localEndPoint = new IPEndPoint(IPAddress.Parse(clientIp), clientPort);
                remoteEndPoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
                    
                TcpClient tcpClient = new(localEndPoint);

                bool connectionEstablished = false;
                while (!connectionEstablished)
                {
                    try
                    {
                        await tcpClient.ConnectAsync(remoteEndPoint);
                        connectionEstablished = true;
                    }
                    catch { }
                }
                Println("Se estableció conexión con el servidor");
                    
                conectionError = false;
                SocketHelper socketHelper = new(tcpClient);
                EnterSystem(socketHelper);

                while (!exitMenu && !conectionError && connected)
                {
                    ShowMenu();
                    try
                    {
                        string option = Read();
                        try
                        {
                            int checkOption = int.Parse(option);
                            if (checkOption > 0 && checkOption <= 8) SendData(socketHelper, option);
                        }
                        catch { }

                        switch (option)
                        {
                            case "1":
                                if (!conectionError) PublishProduct(socketHelper, tcpClient);
                                break;
                            case "2":
                                if (!conectionError) BuyAProduct(socketHelper);
                                break;
                            case "3":
                                if (!conectionError) ModifyAProduct(ref connected, socketHelper, tcpClient);
                                break;
                            case "4":
                                if (!conectionError) DeleteProduct(socketHelper);
                                break;
                            case "5":
                                if (!conectionError) SearchProductByFilter(socketHelper);
                                break;
                            case "6":
                                if (!conectionError) ConsultAProduct(socketHelper, tcpClient);
                                break;
                            case "7":
                                if (!conectionError) RateAProduct(socketHelper, ref connected);
                                break;
                            case "8":
                                Println("Saliendo del programa...");
                                exitMenu = true;
                                break;
                            default:
                                Println("Opción no válida. Por favor, seleccione una opción válida.");
                                break;
                        }
                    }
                    catch (SocketException)
                    {
                        break;
                    }
                    catch (Exception)
                    {
                        break;
                    }

                    Println("\nPresiona cualquier tecla para continuar...");
                    Console.ReadKey();
                }
                if (conectionError || !connected)
                {
                    try
                    {
                        Println("Servidor caido, quiere reintentar?");
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
                            }
                            catch (Exception)
                            {
                                Println("Ingrese 1 o 2 según su eleccón ");
                            }
                        }
                        if (eleccion != 1)
                        {
                            intentoConcetarme = false;
                            tcpClient.Close();
                        }
                        
                    }
                    catch { }
                }
            }
        }
        
        private static async Task DeleteProduct(SocketHelper socketHelper)
        {
            try
            {
                Println("Has seleccionado la opción Baja de producto");
                List<string> userProducts = await GetUserProducts(socketHelper);
                ShowProducts(userProducts);
                Print("Ingrese el nombre del producto a eliminar: ");
                string prodName = Read();
                SendData(socketHelper, prodName);
                var response = await ReceiveData(socketHelper);
                Println(response);
            }
            catch (Exception ex)
            {
                Println(ex.Message);
            }
        }

        private static async Task SearchProductByFilter(SocketHelper socketHelper)
        {
            try
            {
                Println("Has seleccionado la opción Búsqueda de productos");
                Print("Ingrese el nombre para filtrar: ");
                string filterText = Read();
                SendData(socketHelper, filterText);
                var product = "";
                while (product != "end")
                {
                    Println(product);
                    product = await ReceiveData(socketHelper);
                    if (!connected) Println("No se pudo realizar la busqueda.");
                }
            }
            catch (Exception ex)
            {
                Println("Error de conexion");
            }
          
        }

        private static void ModifyAProduct(ref bool connected, SocketHelper socketHelper, TcpClient client)
        {
            try
            {
                Println("Has seleccionado la opción Modificación de producto");
                //CHEQUEAR ESTO
                List<string> productNames = GetUserProducts(socketHelper).Result;
                for (int i = 0; i < productNames.Count; i++)
                {
                    Println($"{productNames[i]}");
                }
                Println("Ingrese nombre del producto a modificar ");
                string productToModifyName = Read();
                while (!productNames.Contains(productToModifyName))
                {
                    Println("Producto no encontrado. Escriba un nombre de producto válido.");
                    productToModifyName = Read();
                }

                bool modificado = false;
                while (!modificado)
                {
                    PrintModifyProductOptions();
                    string attributeOption = Read();
                    while (attributeOption != "1" && attributeOption != "2" && attributeOption != "3" && attributeOption != "4")
                    {
                        Println("Opcion Inválida.");
                        PrintModifyProductOptions();
                        attributeOption = Read();
                    }
                    SendData(socketHelper, productToModifyName);
                    SendData(socketHelper, attributeOption);

                    Print("Inserte nuevo valor:");
                    string newValue = Read();
                    if(attributeOption == "1")
                    {
                        SendData(socketHelper, newValue);
                    }
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
                                
                            }
                            if (value < 0)
                            {
                                Println("El precio debe ser un numero positivo. Inserte nuevamente:");
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
                            catch { }
                            if (value < 0)
                            {
                                Println("El precio debe ser un numero positivo. Inserte nuevamente:");
                                newValue = Read();
                            }
                        }
                        SendData(socketHelper, newValue);
                    }
                    else if (attributeOption == "4")
                    {
                        string imageName = productToModifyName + "InServer.png";
                        var fileCommonHandler = new FileCommsHandler(client);
                        try
                        {
                            SendData(socketHelper, newValue);
                            fileCommonHandler.SendFile(newValue, imageName);
                            
                            Println("Se envio el archivo nuevo al Servidor");
                        }
                        catch (Exception ex)
                        {
                            Println(ex.Message);
                            SendData(socketHelper, "");
                        }
                    }
                    modificado = true;
                }
            }
            catch (Exception ex)
            {
                Println("Error de conexion");
            }
            
        }

        private static void PrintModifyProductOptions()
        {
            Println("Que campo desea modificar: (Digite la opción)");
            Println("1. Descripcion");
            Println("2. Stock Disponible");
            Println("3. Precio");
            Println("4. Imagen");
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
            Print("Seleccione una opción: ");
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

        private static async void SendData(SocketHelper socketHelper, string text)
        {
            if (text == "exit") throw new ExitMenuException();

            byte[] data = Encoding.UTF8.GetBytes(text);
            byte[] dataLength = BitConverter.GetBytes(data.Length);

            try
            {
                await socketHelper.SendAsync(dataLength);
                await socketHelper.SendAsync(data);
            }
            catch (SocketException)
            {
                Println("Error de conexión");
                conectionError = true;
            }
            catch (Exception)
            {
                Println("Error de conexión");
                conectionError = true;
            }
        }

        private static async Task<string> ReceiveData(SocketHelper socketHelper)
        {
            byte[] dataLength = await socketHelper.ReceiveAsync(Protocol.DataSize);
            byte[] data = await socketHelper.ReceiveAsync(BitConverter.ToInt32(dataLength));
            return Encoding.UTF8.GetString(data);
        }

        private static async Task PublishProduct(SocketHelper socketHelper, TcpClient client)
        {
            try
            {
                Println("Has seleccionado la opción Publicación de producto");
                Print("Nombre del producto: ");
                string productName = Read();
                SendData(socketHelper, productName);

                var isOK = await ReceiveData(socketHelper);
                while (isOK != "OK")
                {
                    Print("El producto ya se encuentra en el sistema. Inserte un nombre de producto no publicado: ");
                    productName = Read();
                    SendData(socketHelper, productName);
                    isOK = await ReceiveData(socketHelper);
                }

                Print("Descripción del producto: ");
                string productDescription = Read();
                SendData(socketHelper, productDescription);

                int stock;
                bool stockCorrect = false;
                while (!stockCorrect)
                {
                    Print("Cantidad disponible: ");
                    string input = Read();

                    if (int.TryParse(input, out stock) && stock >= 0)
                    {
                        stockCorrect = true;
                        SendData(socketHelper, input);
                    }
                    else
                    {
                        Println("Ingrese un número entero válido mayor o igual a 0.");
                    }
                }

                int precioProducto = 0;
                bool priceCorrect = false;
                while (!priceCorrect)
                {
                    Print("Precio del producto: ");
                    string input = Read();

                    if (int.TryParse(input, out precioProducto) && precioProducto > 0)
                    {
                        priceCorrect = true;
                        SendData(socketHelper, input);
                    }
                    else
                    {
                        Println("Ingrese un número válido mayor a 0.");
                    }
                }
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
                    string path = Read();
                    string imageName = productName + "InServer.png";

                    var fileCommonHandler = new FileCommsHandler(client);
                    try
                    {
                        fileCommonHandler.SendFile(path, imageName);
                        SendData(socketHelper, path);
                        Println("Se envio el archivo al Servidor");
                    }
                    catch (Exception ex)
                    {
                        Println(ex.Message);
                        SendData(socketHelper, "");
                    }
                }
                else
                {
                    SendData(socketHelper, eleccion + "");
                    SendData(socketHelper, "sin imagen");
                }
                var resultCreate = await ReceiveData(socketHelper);
                if (connected && resultCreate == "OK") Println("Producto agregado con éxito.");
                else Println("No se pudo agregar el producto");
            }
            catch
            {
                Println("Error de conexion");
            }
        }

        private static void EnterSystem(SocketHelper socketHelper)
        {
            Println("Ingrese la opción:");
            Println("1. Iniciar Sesión");
            Println("2. Registrarse");

            string initOption = Read();

            while (initOption != "1" && initOption != "2")
            {

                Println("Ingrese una opcion valida.");

                initOption = Read();

            }
            try
            {
                SendData(socketHelper, initOption);

                if (initOption == "1") LogIn(socketHelper);

                else RegisterUser(socketHelper);
            }
            catch (Exception ex)
            {
                Println("No fue posible acceder al servidor");
                conectionError = true;
            }

        }

        private static async void RegisterUser(SocketHelper socketHelper)
        {
            Print("Ingrese nombre de usuario:   ");
            string username = Read();
            Print("Ingrese constraseña:   ");
            string password = Read();
            while (password == "")
            {
                Print("La contraseña no puede ser vacía. Ingrese una contraseña nuevamente:  ");
                password = Read();
            }
            try
            {
                string user = username + "@" + password;
                SendData(socketHelper, user);
                var response = await ReceiveData(socketHelper);
                if (connected)
                {
                    if (response.ToString() == "OK")
                    {
                        Println("Bienvenido al sistema " + username);
                        conectionError = false;
                    }
                    else
                    {
                        Println("Ya hay un usuario con ese nombre en el sistema. Intente nuevamente.");
                        RegisterUser(socketHelper);
                    }
                }
            }
            catch
            {
                conectionError = true;
                Println("Conexión con el servidor perdida.");
            }
        }

        private static void RateAProduct(SocketHelper socketHelper, ref bool connected)
        {
            try
            {
                Println("Has seleccionado la opción Calificar un producto");

                //CHEQUEAR ESTO
                List<string> productsToRate = GetUserProducts(socketHelper).Result;
                ShowProducts(productsToRate);

                Print("Ingrese el nombre del producto a calificar: ");
                string prodName = Read();
                productsToRate = productsToRate.Select(product => product.Split('|')[0].Trim()).ToList();

                bool productExists = productsToRate.Contains(prodName);
                while (!productExists)
                {
                    Print("Ingrese alguna de las opciones listadas: ");
                    prodName = Read();
                    if (productsToRate.Contains(prodName))
                    {
                        productExists = true;
                    }
                }
               
                SendData(socketHelper, prodName);

                Println("¿Cuál es su opinión del producto?");
                var opinion = Read();
                SendData(socketHelper, opinion);

                string input = "";
                while (true)
                {
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
                Println("Error de conexion");
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

        private static async Task<List<string>> GetUserProducts(SocketHelper socketHelper)
        {
            var strQuantProducts = await ReceiveData(socketHelper);
            int quantProducts = int.Parse(strQuantProducts);

            List<string> products = new List<string>();

            for (int i=0; i<quantProducts; i++)
            {
                var prod = await ReceiveData(socketHelper);
                products.Add(prod);
            }

            return products;
        }

        private static async Task<List<NameStock>> GetProductsToBuy(SocketHelper socketHelper)
        {
            var products = new List<NameStock>();
            var reception = await ReceiveData(socketHelper);
            while (connected && reception != "end")
            {
                NameStock product = new NameStock()
                {
                    Name = reception.Split("@")[0],
                    Stock = reception.Split("@")[1]
                };
                products.Add(product);
                Console.WriteLine("Producto: {0}   |   Stock: {1}", product.Name, product.Stock);
                reception = await ReceiveData(socketHelper);
            }
            return products;
        }

        private static async Task ConsultAProduct(SocketHelper socketHelper, TcpClient client)
        {
            try
            {
                Println("Has seleccionado la opción Consultar un producto específico");
                Println("Productos disponibles:");
                //CHEQUEAR ESTO
                List<string> productsToConsult = GetUserProducts(socketHelper).Result;
                ShowProducts(productsToConsult);
                Print("Ingrese el nombre del producto que quiera consultar: ");
                string prodName = Read();
                productsToConsult = productsToConsult.Select(product => product.Split('|')[0].Trim()).ToList();

                bool productExists = productsToConsult.Contains(prodName);
                while (!productExists)
                {
                    Print("Ingrese alguna de las opciones listadas: ");
                    prodName = Read();
                    if (productsToConsult.Contains(prodName))
                    {
                        productExists = true;
                    }
                }
                Println("Información sobre el producto: " + prodName);
                SendData(socketHelper, prodName);

                var consultedProduct = await ReceiveData(socketHelper);
                Println(consultedProduct);

                var image = await ReceiveData(socketHelper);
                if (image != "sin imágen")
                {
                    var imageToDelete = prodName + "InClient.png";
                    FileStreamHandler.Delete(imageToDelete, settingMng.ReadSettings(ClientConfig.clientImageRouteKey));

                    Println("Antes de recibir el archivo");
                    var fileCommonHandler = new FileCommsHandler(client);
                    fileCommonHandler.ReceiveFile(settingMng.ReadSettings(ClientConfig.clientImageRouteKey));
                    string imageName = prodName;
                    var productImage = await ReceiveData(socketHelper);
                    if (productImage == "error")
                    {
                        Println("La imagen no fue encontrada.");
                    }
                    else
                    {
                        Println("Archivo recibido!!");
                    }
                }
            }
            catch (Exception ex)
            {
                Println("Error de conexion");
            }
        }

        private static async Task BuyAProduct(SocketHelper socketHelper)
        {
            try
            {
                Println("Has seleccionado la opción Compra de productos");
                bool isBought = false;
                //CHEQUEAR ESTO
                List<NameStock> products = await GetProductsToBuy(socketHelper);
                while (!isBought)
                {
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
                                    var message = productNameStock.Name + "@" + amountToBuy.ToString();
                                    SendData(socketHelper, message);
                                    message = await ReceiveData(socketHelper);
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
                Println("Error de conexion");
            }
        }

        private static async Task LogIn(SocketHelper socketHelper)
        {
            try
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

                    var response = await ReceiveData(socketHelper);

                    if (response == "ok")
                    {
                        Println("Bienvenido al sistema " + userName);
                        correctUser = true;
                        conectionError = false;
                    }
                    else
                    {
                        Println(response);
                    }
                }
            }
            catch (Exception ex)
            {
                Println("No fue posible acceder al servidor");
                conectionError = true;
            }
        }
    }

}

