using Common;
using Exceptions;
using Protocolo;
using Domain;
using System.Net;
using System.Net.Sockets;
using System.Text;
using DTOs;

namespace Servidor
{
    public class ProgramServidor
    {
        static readonly SettingsManager settingMng = new SettingsManager();
        private static object locker = new object();
        private static bool isServerOn = true;
        private static List<Product> products = new List<Product>();
        private static List<User> users = new List<User>();
        private static List<Purchase> compras = new List<Purchase>();

        private static ProgramServidor _instance;

        public bool acceptingConnections;
        private readonly TcpListener tcpListener;

        public static ProgramServidor Instance
        {
            get
            {
                lock (locker)
                {
                    if (_instance == null)
                    {
                        throw new ExitException("No se pudo conectar con el servidor.");
                    }
                    return _instance;
                }
            }
        }
        
        public static void SetInstance(ProgramServidor instance)
        {
            lock (locker)
            {
                if (_instance == null)
                {
                    _instance = instance;
                }
            }
        }
       
        public ProgramServidor(string serverIpAddress, string serverPort)
        {

            var localEndPoint = new IPEndPoint(
                 IPAddress.Parse(serverIpAddress), int.Parse(serverPort));

            tcpListener = new TcpListener(localEndPoint);
            acceptingConnections = true;

        }
        public async Task StartReceivingConnections()
        {
            
            List<TcpClient> activeClients = new();
            
            LoadTestData();

            //string serverIp = settingMng.ReadSettings(ServerConfig.serverIPconfigKey);
            //int serverPort = int.Parse(settingMng.ReadSettings(ServerConfig.serverPortconfigKey));
            //var localEndPoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);

            //TcpListener tcpListener = new(localEndPoint);
            Console.WriteLine("######### Server Tcp Iniciado y aceptando conexiones ###########");
            tcpListener.Start();
            Println("Esperando por clientes....");
            int connectedClients = 0;

            var server = Task.Run(async () => await HandleServer(tcpListener));

            while (isServerOn) 
            {
                try
                {
                    TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync();
                    activeClients.Add(tcpClient);
                    Println("Se conecto un cliente..");
                    connectedClients++;
                    var thread = Task.Run(async () => await HandleClientAsync(tcpClient, connectedClients, tcpListener));
                }
                catch (Exception)
                {
                    Println("Cerrando Servidor");
                }
            }

            for(int i = 0; i< activeClients.Count; i++)
            {
                activeClients[i].Close();
            }

            if (isServerOn)
            {
                tcpListener.Stop();
                Println("Servidor apagado");
            }
            
        }

        static async Task HandleClientAsync(TcpClient client, int nroClient, TcpListener listener) 
        {
            bool connected = true;
            SocketHelper socketHelper = new(client);

            var initOption = await ReceiveData(socketHelper);
            if (connected)
            {
                try
                {
                    if (initOption == "1")
                    {
                        await LogIn(socketHelper);
                    }
                    else
                    {
                        var newUser = await RegisterUser(socketHelper);
                        lock (locker)
                        {
                            users.Add(newUser);
                        }
                    }
                }
                catch (ExitException) 
                {
                    connected = false;
                }
            }

            int option = 0;
            while (connected)
            {
                try
                {
                    var strOption = await ReceiveData(socketHelper);
                    option = int.Parse(strOption);

                    switch (option)
                    {
                        case 1:
                            await CreateProduct(socketHelper, client);
                            break;
                        case 2:
                            await BuyProduct(socketHelper);
                            break;
                        case 3:
                            await ModifyProduct(socketHelper, client);
                            break;
                        case 4:
                            await DeleteProduct(socketHelper);
                            break;
                        case 5:
                            await SearchForProducts(socketHelper);
                            break;
                        case 6:
                            await ConsultProduct(socketHelper, client);
                            break;
                        case 7:
                            await RateAProduct(socketHelper);
                            break;
                        case 8:
                            break;
                        default:
                            Println("Opción no válida.");
                            break;
                    }
                }
                catch (ExitException) { }
                catch (ShutServerDownException)
                {
                    isServerOn = false;
                    listener.Stop();
                }
            }
            Console.WriteLine("Cliente {0} desconectado", nroClient);
        }
        
        static async Task HandleServer(TcpListener listener)
        {
            while (isServerOn)
            {
                Println("Ingrese exit para cerrar el Server");
                string entrada = Read();
                if (entrada.Equals("exit"))
                {
                    ApagarServidor();
                    try
                    {
                        listener.Stop();
                    }
                    catch (Exception)
                    {
                        Println("Apagando servidor");
                    }
                }
            }
        }
        
        private static async Task SendClientProducts(SocketHelper socketHelper)
        {
            try
            {
                List<Product> productosDelCliente;
                lock (locker)
                {
                    productosDelCliente = products.Where(prod => prod.OwnerUserName == socketHelper.UserName).ToList();
                }
                await SendData(socketHelper, productosDelCliente.Count.ToString());

                for (int i = 0; i < productosDelCliente.Count; i++)
                {
                    string productoMostrado = productosDelCliente.ElementAt(i).Name;
                    await SendData(socketHelper, productoMostrado);
                }

            }catch(Exception)
            {
                return;
            }
            
        }

        private static async Task SendProductsNameStock(SocketHelper socketHelper)
        {
            List<string> productNames = new();
            lock (locker)
            {
                foreach (Product product in products)
                {
                    if (product.Stock > 0)
                    {
                        string name = product.Name + "@" + product.Stock.ToString();
                        productNames.Add(name);
                    }
                }
            }
            foreach (string name in productNames)
            {
                await SendData(socketHelper, name);
            }
            await SendData(socketHelper, "end");
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

        private static async Task SendData(SocketHelper socketHelper, string text)
        {
            byte[] data = Encoding.UTF8.GetBytes(text);
            byte[] dataLength = BitConverter.GetBytes(data.Length);

            if (isServerOn)
            {
                try
                {
                    await socketHelper.SendAsync(dataLength);
                    await socketHelper.SendAsync(data);
                }
                catch (SocketException)
                {
                    Println("Error de conexión");
                }
            }
        }

        private static async Task<string> ReceiveData(SocketHelper socketHelper)
        {
            byte[] dataLength = await socketHelper.ReceiveAsync(Protocol.DataSize);
            byte[] data = await socketHelper.ReceiveAsync(BitConverter.ToInt32(dataLength));
            string received = Encoding.UTF8.GetString(data);
            if (received.ToLower().Equals("exit")) throw new ExitException();
            return received;
        }

        private static async Task LogIn(SocketHelper socketHelper)
        {
            bool correctUser = false;
            while (!correctUser && isServerOn)
            {
                var user = await ReceiveData(socketHelper);
                correctUser = UserIsCorrect(user);

                if (correctUser)
                {
                    await SendData(socketHelper, "ok");
                    socketHelper.UserName = user.Split('#')[0];
                    Println(socketHelper.UserName + " conectado");
                    correctUser = true;
                }
                else
                {
                    await SendData(socketHelper, "Nombre de usuario y/o contraseña incorrecto/s. Vuelva a intentarlo.");
                }
            }
        }

        private static bool UserIsCorrect(string user)
        {
            string userName;
            string userPass;
            try
            {
                userName = user.Split('#')[0];
                userPass = user.Split('#')[1];

            }
            catch (Exception)
            {
                return false;
            }

            foreach (User usr in users)
            {
                if (usr.Username == userName && usr.Password == userPass)
                {
                    userName = usr.Username;
                    return true;
                }
            }
            return false;
        }

        private static async Task CreateProduct(SocketHelper socketHelper, TcpClient client)
        {
            var productName = await ReceiveData(socketHelper);
            bool isInProducts;
            lock (locker)
            {
                isInProducts = ExistsProductName(productName);
            }
            while (isInProducts)
            {
                await SendData(socketHelper, "Error: Producto ya ingresado al sistema.");
                productName = await ReceiveData(socketHelper);
                lock (locker)
                {
                    isInProducts = ExistsProductName(productName);
                }
            }
            await SendData(socketHelper, "OK");

            var productDescription = await ReceiveData(socketHelper);

            var strProductStock = await ReceiveData(socketHelper);
            int productStock = int.Parse(strProductStock);

            var strProductPrice = await ReceiveData(socketHelper);
            int productPrice = int.Parse(strProductPrice);

            var strHasImage = await ReceiveData(socketHelper);
            int hasImage = int.Parse(strHasImage);
            var productImage = "";
            string imageName;

            if (hasImage == 1)
            {
                var fileCommonHandler = new FileCommsHandler(client);
                try
                {
                    await fileCommonHandler.ReceiveFile(settingMng.ReadSettings(ServerConfig.serverImageRouteKey));
                    imageName = productName + ".png";
                    productImage = await ReceiveData(socketHelper);
                    if (productImage.Equals("")) imageName = "sin imagen";
                    await SendData(socketHelper, "ok");
                }
                catch (InvalidRouteException ex)
                {
                    Println(ex.Message + " Ingrese una ruta correcta y vuelva a iniciar el servidor");
                    await SendData(socketHelper, "routeError");
                    throw new ShutServerDownException();
                }
            }
            else
            {
                imageName = "sin imagen";
                productImage = await ReceiveData(socketHelper);
                Println("");
            }
            Product product = new()
            {
                Name = productName,
                Description = productDescription,
                Price = productPrice,
                Stock = productStock,
                Image = imageName,
                OwnerUserName = socketHelper.UserName
            };
            bool productAlreadyExists = false;
            lock (locker)
            {
                productAlreadyExists = ExistsProduct(product);
            }
            if (productAlreadyExists)
            {
                lock (locker)
                {
                    AddProduct(product);
                }
                await SendData(socketHelper, "OK");
                Println(socketHelper.UserName + " agrego un producto");
            }
        }

        public Product CreateProduct(Product product)
        {
           
            if (ExistsProductName(product.Name)) throw new ArgumentException("El producto ya existe.");
            products.Add(product);
            return product;
        }

        public static void AddProduct(Product product)
        {
            products.Add(product);
        }

        public  void AddPurchase(Purchase purchase)
        {
            compras.Add(purchase);
        }

        public static bool ExistsProduct(Product product)
        {
            return !products.Contains(product);
        }

        public static bool ExistsProductName(string productName)
        {
            return products.Exists(product => product.Name.ToLower().Equals(productName.ToLower()));
        }

        private static async Task<User> RegisterUser(SocketHelper socketHelper)
        {
            var user = await ReceiveData(socketHelper);

            bool userExists = true;
            lock (locker)
            {
                userExists = users.Exists(u => u.Username == user.Split("@")[0]);
            }
            
            while (userExists)
            {
                await SendData(socketHelper, "Error: Username ya en sistema.");
                user = await ReceiveData(socketHelper);
            }
            User newUser = new()
            {
                Id = users.Count + 1,
                Username = user.Split("@")[0],
                Password = user.Split("@")[1]
            };
            socketHelper.UserName = newUser.Username;
            await SendData(socketHelper, "OK");
            Println(socketHelper.UserName + " se ha registrado e iniciado sesion.");
            return newUser;
        }

        private static async Task SendProducts(SocketHelper socketHelper, List<Product> products)
        {
            string quantProducts;
            lock (locker)
            {
                quantProducts = products.Count.ToString();
            }
            await SendData(socketHelper, quantProducts);

            foreach (Product prod in products)
            {
                if (prod.Stock > 0)
                {
                    await SendData(socketHelper, prod.Name + " | Stock: " + prod.Stock);
                }
                else
                {
                    await SendData(socketHelper, prod.Name);
                }
            }
        }

        private static async Task SendProducts(SocketHelper socketHelper)
        {
            string quantProducts;
            lock (locker)
            {
                quantProducts = products.Count.ToString();
            }
            await SendData(socketHelper, quantProducts);

            List<string> prodNamesWithStock = new();
            lock (locker)
            {
                foreach (Product prod in products)
                {
                    prodNamesWithStock.Add(prod.Name + " | Stock: " + prod.Stock);
                }
            }
            foreach (string name in prodNamesWithStock)
            {
                await SendData(socketHelper, name);
            }
        }

        public static List<Product> GetClientProducts(string clientUserName)
        {
            List<Product> clientProducts;
            lock (locker)
            {
                clientProducts = products.Where(prod => prod.OwnerUserName == clientUserName).ToList();
            }
            return clientProducts;
        }

        private static async Task BuyProduct(SocketHelper socketHelper)
        {
            await SendProductsNameStock(socketHelper);

            var message = await ReceiveData(socketHelper);
            string nameProduct = message.Split('@')[0];
            string amountBought = message.Split('@')[1];
            Product productBought;
            lock (locker)
            {
                productBought = products.Find(p => p.Name == nameProduct);
            }
            int stock = productBought.Stock - int.Parse(amountBought);
            if (stock < 0) await SendData(socketHelper, "error");
            else
            {
                productBought.Stock -= int.Parse(amountBought);
                Println(socketHelper.UserName + " compró " + amountBought + " unidades de " + nameProduct);
                await SendData(socketHelper, "ok");
            }
        }

        private static async Task ModifyProduct(SocketHelper socketHelper, TcpClient client)
        {
            await SendClientProducts(socketHelper);

            var productName = await ReceiveData(socketHelper);
            Product productToModify;
            lock (locker)
            {
                productToModify = products.Find(p => p.Name == productName);
            }

            var atributeOption = await ReceiveData(socketHelper);

            var newValue = await ReceiveData(socketHelper);

            switch (atributeOption)
            {
                case "1":
                    productToModify.Description = newValue;
                    break;
                case "2":
                    productToModify.Stock = int.Parse(newValue);
                    break;
                case "3":
                    productToModify.Price = int.Parse(newValue);
                    break;
                case "4":
                    var imageToDelete = productToModify.Name + "InServer.png";
                    FileStreamHandler.Delete(imageToDelete, settingMng.ReadSettings(ServerConfig.serverImageRouteKey));

                    var fileCommonHandler2 = new FileCommsHandler(client);
                    await fileCommonHandler2.ReceiveFile(settingMng.ReadSettings(ServerConfig.serverImageRouteKey));
                    string imageName = productName + ".png";
                    string productImage = await ReceiveData(socketHelper);
                    if (productImage.Equals("")) imageName = "sin imagen";
                    productToModify.Image = imageName;
                    break;
            }
            Println(socketHelper.UserName + " modificó el producto " + productName);
        }

        private static async Task DeleteProduct(SocketHelper socketHelper)
        {
            List<Product> clientProducts = GetClientProducts(socketHelper.UserName);
            await SendProducts(socketHelper, clientProducts);
            var prodToDelete = await ReceiveData(socketHelper);

            lock (locker)
            {
                products = products.Where(prod => !(prod.Name.Equals(prodToDelete))).ToList();
            }
            await SendData(socketHelper, "Se ha eliminado el producto correctamente");
            FileStreamHandler.Delete(prodToDelete, settingMng.ReadSettings(ServerConfig.serverImageRouteKey));
            Println(socketHelper.UserName + " eliminó el producto " + prodToDelete);
        }

        private static async Task SearchForProducts(SocketHelper socketHelper)
        {
            var filterText = await ReceiveData(socketHelper);

            List<string> filteredProds = new();
            lock (locker)
            {
                foreach (var prod in products)
                {
                    if (prod.Name.Contains(filterText))
                    {
                        filteredProds.Add(prod.ToString());
                    }
                }
            }
            foreach (string prod in filteredProds)
            {
                await SendData(socketHelper, prod);
            }

            await SendData(socketHelper, "end");
        }

        private static async Task ConsultProduct(SocketHelper socketHelper, TcpClient client)
        {
            await SendProducts(socketHelper, products);

            var productToConsult = await ReceiveData(socketHelper);

            Product consultedProduct;
            lock (locker)
            {
                consultedProduct = products.FirstOrDefault(prod => prod.Name == productToConsult);
            }
            await SendData(socketHelper, consultedProduct.ToString());
            await SendData(socketHelper, consultedProduct.Image);
            if (consultedProduct.Image != "sin imagen")
            {
                var fileCommonHandler = new FileCommsHandler(client);
                try
                {
                    string image = consultedProduct.Name + "InServer.png";
                    string searchDirectory = @settingMng.ReadSettings(ServerConfig.serverImageRouteKey);
                    string[] imageFiles = Directory.GetFiles(searchDirectory, $"{image}.*");
                    string path = imageFiles[0];

                    await fileCommonHandler.SendFile(path, consultedProduct.Name + "InClient.png");
                    await SendData(socketHelper, image);

                    Println("Se envio el archivo al Cliente");
                }
                catch (NonexistingFileException)
                {
                    Println("Imagen no encontrada");
                    await SendData(socketHelper, "error");
                }
            }
        }

        private static async Task RateAProduct(SocketHelper socketHelper)
        {
            await SendProducts(socketHelper);
            var strProductName = await ReceiveData(socketHelper);

            var opinion = await ReceiveData(socketHelper);

            var strRating = await ReceiveData(socketHelper);
            int rating = int.Parse(strRating);

            Review review = new Review()
            {
                UserName = socketHelper.UserName,
                Comment = opinion,
                Rating = rating,
            };

            lock (locker)
            {
                foreach (var prod in products)
                {
                    if (prod.Name == strProductName)
                    {
                        prod.Reviews.Add(review);
                        break;
                    }
                }
            }
            Println(socketHelper.UserName + " calificó un producto.");
        }

        private static void LoadTestData()
        {
            User user1 = new()
            {
                Id = 2,
                Username = "Nahuel",
                Password = "Nah123"
            };
            users.Add(user1);
            User user2 = new()
            {
                Id = 1,
                Username = "Alan",
                Password = "Alan123"
            };
            users.Add(user2);
            User user3 = new()
            {
                Id = 3,
                Username = "Lucas",
                Password = "Luc123"
            };
            users.Add(user3);

            Product prod1 = new()
            {
                Id = 1,
                OwnerUserName = user1.Username,
                Name = "Mesa",
                Description = "Primera mesa de Nahuel",
                Stock = 4,
                Price = 252,
                Image = "sin imagen"
            };
            products.Add(prod1);
            Product prod2 = new()
            {
                Id = 2,
                OwnerUserName = user1.Username,
                Name = "Silla",
                Description = "Primera silla de Nahuel",
                Stock = 3,
                Price = 134,
                Image = "sin imagen"
            };
            products.Add(prod2);
            Product prod3 = new()
            {
                Id = 3,
                OwnerUserName = user2.Username,
                Name = "Cama",
                Description = "Primera cama de Alan",
                Stock = 7,
                Price = 300,
                Image = "sin imagen"
            };
            products.Add(prod3);
            Product prod4 = new()
            {
                Id = 4,
                OwnerUserName = user3.Username,
                Name = "Escritorio",
                Description = "Primer escritorio de Lucas",
                Stock = 1,
                Price = 450,
                Image = "sin imagen"
            };
            products.Add(prod4);
        }

        static void ApagarServidor()
        {
            isServerOn = false;
        }
    }
}