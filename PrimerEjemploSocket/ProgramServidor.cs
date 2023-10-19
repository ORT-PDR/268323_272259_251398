using Common;
using Protocolo;
using Servidor;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PrimerEjemploSocket
{
    public class ProgramServidor
    {
        static readonly SettingsManager settingMng = new SettingsManager();
        private static object locker = new object();
        private static bool isServerOn = true;

        static async Task Main(string[] args)
        {
            List<Product> products = new List<Product>();
            List<User> users = new List<User>();
            List<TcpClient> activeClients = new();
            
            LoadTestData(ref users, ref products);

            string serverIp = settingMng.ReadSettings(ServerConfig.serverIPconfigKey);
            int serverPort = int.Parse(settingMng.ReadSettings(ServerConfig.serverPortconfigKey));
            var localEndPoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);

            TcpListener listener = new(localEndPoint);
            listener.Start();
            Println("Esperando por clientes....");
            int connectedClients = 0;

            var server = Task.Run(async () => await HandleServer(listener));

            while (isServerOn) 
            {
                try
                {
                    TcpClient tcpClient = await listener.AcceptTcpClientAsync();
                    activeClients.Add(tcpClient);
                    Println("Se conecto un cliente..");
                    connectedClients++;
                    var thread = Task.Run(async () => await HandleClientAsync(tcpClient, connectedClients, users, products));
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
                listener.Stop();
                Println("Servidor apagado");
            }
            
        }

        static async Task HandleClientAsync(TcpClient client, int nroClient, List<User> users, List<Product> products) 
        {
            bool connected = true;
            SocketHelper socketHelper = new(client);

            var initOption = await ReceiveData(socketHelper);
            if (connected)
            {
                if (initOption == "1")
                {
                    LogIn(socketHelper, users);
                }
                else
                {
                    //CHEQUEAR ESTO
                    var newUser = RegisterUser(socketHelper, users).Result;
                    users.Add(newUser);
                }
            }

            int option = 0;
            while (connected)
            {
                var strOption = await ReceiveData(socketHelper);
                if (!connected) break;
                option = int.Parse(strOption);
                switch (option)
                {
                    case 1:
                        //Publicación de producto
                        //CHEQUEAR ESTO
                        var product = CreateProduct(socketHelper, products, client).Result;
                        bool productAlreadyExists = false;
                        lock (locker)
                        {
                            productAlreadyExists = !products.Contains(product);
                        }
                        if (productAlreadyExists)
                        {
                            lock (locker)
                            {
                                products.Add(product);
                            }
                            SendData(socketHelper, "OK");
                            Println(socketHelper.UserName + " agrego un producto");
                        }
                        break;
                    case 2:
                        //Compra de productos
                        SendProductsNameStock(socketHelper, products);

                        var message = await ReceiveData(socketHelper);
                        if (connected)
                        {
                            string nameProduct = message.Split('@')[0];
                            string amountBought = message.Split('@')[1];
                            Product productBought;
                            lock (locker)
                            {
                                productBought = products.Find(p => p.Name == nameProduct);
                            }
                            int stock = productBought.Stock - int.Parse(amountBought);
                            if (stock < 0) SendData(socketHelper, "error");
                            else
                            {
                                productBought.Stock -= int.Parse(amountBought);
                                Println(socketHelper.UserName + " compró " + amountBought + " unidades de " + nameProduct);
                                SendData(socketHelper, "ok");
                            }
                        }
                        break;

                    case 3:
                        //Modificación de producto
                        SendClientProducts(products, socketHelper);

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
                                
                                Println("Antes de recibir el archivo nuevo");
                                var fileCommonHandler2 = new FileCommsHandler(client);
                                fileCommonHandler2.ReceiveFile(settingMng.ReadSettings(ServerConfig.serverImageRouteKey));
                                string productImage = productName + ".png";
                                productToModify.Image = productImage;

                                Println("Archivo nuevo recibido!!");

                                break;
                        }
                        Println(socketHelper.UserName + " modificó el producto " + productName);
                        break;
                    case 4:
                        //Baja de producto
                        List<Product> clientProducts = GetClientProducts(products, socketHelper.UserName);
                        SendProducts(socketHelper, clientProducts);
                        DeleteProduct(socketHelper, products);

                        break;
                    case 5:
                        //Búsqueda de productos
                        var filterText = await ReceiveData(socketHelper);

                        lock (locker)
                        {
                            foreach (var prod in products)
                            {
                                if (prod.Name.Contains(filterText))
                                {
                                    SendData(socketHelper, prod.ToString());
                                    Println(prod.Name);
                                }
                            }
                        }
                        
                        SendData(socketHelper, "end");

                        break;
                    case 6:
                        //consultar un producto específico
                        SendProducts(socketHelper, products);

                        var productToConsult = await ReceiveData(socketHelper);

                        Product consultedProduct;
                        lock (locker)
                        {
                            consultedProduct = products.FirstOrDefault(prod => prod.Name == productToConsult);
                        }
                        SendData(socketHelper, consultedProduct.ToString());
                        SendData(socketHelper, consultedProduct.Image);
                        if (consultedProduct.Image != "sin imágen")
                        {
                            var fileCommonHandler = new FileCommsHandler(client);
                            try
                            {
                                string image = consultedProduct.Name + "InServer.png";
                                string searchDirectory = @settingMng.ReadSettings(ServerConfig.serverImageRouteKey);
                                string[] imageFiles = Directory.GetFiles(searchDirectory, $"{image}.*");
                                string path = imageFiles[0];

                                fileCommonHandler.SendFile(path, consultedProduct.Name + "InClient.png");
                                SendData(socketHelper, image);

                                Println("Se envio el archivo al Cliente");
                            }
                            catch (Exception ex)
                            {
                                Println("Imagen no encontrada");
                                SendData(socketHelper, "error");
                            }
                        }
                        break;

                    case 7:
                        SendProducts(socketHelper, products);
                        RateAProduct(socketHelper, products);

                        break;
                    
                    case 8:
                        
                        break;

                    default:
                        Println("Opción no válida.");
                        break;
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
                    catch (Exception ex)
                    {
                        Println("Apagando srvidor");
                    }
                }
            }
        }
        
        private static void SendClientProducts(List<Product> products, SocketHelper socketHelper)
        {
            try
            {
                List<Product> productosDelCliente;
                lock (locker)
                {
                    productosDelCliente = products.Where(prod => prod.OwnerUserName == socketHelper.UserName).ToList();
                }
                SendData(socketHelper, productosDelCliente.Count.ToString());

                for (int i = 0; i < productosDelCliente.Count; i++)
                {
                    string productoMostrado = productosDelCliente.ElementAt(i).Name;
                    SendData(socketHelper, productoMostrado);
                }

            }catch(Exception ex)
            {
                return;
            }
            
        }

        private static void SendProductsNameStock(SocketHelper socketHelper, List<Product> products)
        {
            lock (locker)
            {
                try
                {
                    foreach (Product product in products)
                    {
                        if (product.Stock > 0)
                        {
                            string mensaje = product.Name + "@" + product.Stock.ToString();
                            SendData(socketHelper, mensaje);
                        }
                    }
                } catch {
                    return;
                }   
            }
            SendData(socketHelper, "end");
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
            return Encoding.UTF8.GetString(data);
        }

        private static async Task LogIn(SocketHelper socketHelper, List<User> users)
        {
            bool correctUser = false;
            while (!correctUser && isServerOn)
            {
                var user = await ReceiveData(socketHelper);
                correctUser = UserIsCorrect(user, users);

                if (correctUser)
                {
                    SendData(socketHelper, "ok");
                    socketHelper.UserName = user.Split('#')[0];
                    Println(socketHelper.UserName + " conectado");
                    correctUser = true;
                }
                else
                {
                    SendData(socketHelper, "Nombre de usuario y/o contraseña incorrecto/s. Vuelva a intentarlo.");
                }
            }
        }

        private static bool UserIsCorrect(string user, List<User> users)
        {
            string userName = "";
            string userPass = "";
            try
            {
                userName = user.Split('#')[0];
                userPass = user.Split('#')[1];

            }
            catch (Exception e)
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

        private static async Task<Product> CreateProduct(SocketHelper socketHelper, List<Product> products, TcpClient client)
        {
            try
            {
                var productName = await ReceiveData(socketHelper);
                bool isInProducts = products.Exists(product => product.Name.ToLower().Equals(productName.ToLower()));
                while (isInProducts)
                {
                    SendData(socketHelper, "Error: Producto ya ingresado al sistema.");
                    productName = await ReceiveData(socketHelper);
                    isInProducts = products.Exists(product => product.Name.ToLower().Equals(productName.ToLower()));

                }
                SendData(socketHelper, "OK");

                var productDescription = await ReceiveData(socketHelper);

                var strProductStock = await ReceiveData(socketHelper);
                int productStock = int.Parse(strProductStock);

                var strProductPrice = await ReceiveData(socketHelper);
                int productPrice = int.Parse(strProductPrice);

                var strHasImage = await ReceiveData(socketHelper);
                int hasImage = int.Parse(strHasImage);
                var productImage = "";
                string imageName = "";

                if (hasImage == 1)
                {
                    Println("Antes de recibir el archivo");
                    var fileCommonHandler = new FileCommsHandler(client);
                    fileCommonHandler.ReceiveFile(settingMng.ReadSettings(ServerConfig.serverImageRouteKey));
                    imageName = productName + ".png";
                    productImage = await ReceiveData(socketHelper);


                    Println("Archivo recibido!!");
                }
                else
                {
                    imageName = "sin imágen";
                    productImage = await ReceiveData(socketHelper);
                    Println("");
                }
                Product product = new Product();
                product.Name = productName;
                product.Description = productDescription;
                product.Price = productPrice;
                product.Stock = productStock;
                product.Image = imageName;
                product.OwnerUserName = socketHelper.UserName;
                return product;
            }
            catch(Exception ex)
            {
                return null;
            }
        }

        private static async Task<User> RegisterUser(SocketHelper socketHelper, List<User> users)
        {
            var user = await ReceiveData(socketHelper);

            while (users.Exists(u => u.Username == user.Split("@")[0]))
            {
                SendData(socketHelper, "Error: Username ya en sistema.");
                user = await ReceiveData(socketHelper);
            }
            User newUser = new()
            {
                Id = users.Count + 1,
                Username = user.Split("@")[0],
                Password = user.Split("@")[1]
            };
            socketHelper.UserName = newUser.Username;
            SendData(socketHelper, "OK");
            Println(socketHelper.UserName + " se ha registrado e iniciado sesion.");
            return newUser;
        }

        private static void SendProducts(SocketHelper socketHelper, List<Product> products)
        {
            string quantProducts;
            lock (locker)
            {
                quantProducts = products.Count.ToString();
            }
            SendData(socketHelper, quantProducts);

            lock (locker)
            {
                foreach (Product prod in products)
                {
                    if (prod.Stock > 0)
                    {
                        SendData(socketHelper, prod.Name + " | Stock: " + prod.Stock);
                    }
                    else
                    {
                        SendData(socketHelper, prod.Name);
                    }
                }
            }
        }

        private static List<Product> GetClientProducts(List<Product> products, string clientUserName)
        {
            List<Product> clientProducts;
            lock (locker)
            {
                clientProducts = products.Where(prod => prod.OwnerUserName == clientUserName).ToList();
            }
            return clientProducts;
        }

        private static async Task DeleteProduct(SocketHelper socketHelper, List<Product> products)
        {
            var prodToDelete = await ReceiveData(socketHelper);

            lock (locker)
            {
                products = products.Where(prod => !(prod.Name.Equals(prodToDelete))).ToList();
            }
            SendData(socketHelper, "Se ha eliminado el producto correctamente");
            FileStreamHandler.Delete(prodToDelete, settingMng.ReadSettings(ServerConfig.serverImageRouteKey));
            Println(socketHelper.UserName + " eliminó el producto " + prodToDelete);
        }

        private static async Task RateAProduct(SocketHelper socketHelper, List<Product> products)
        {
            try
            {
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
                Println(socketHelper.UserName + "calificó un producto.");
            }
            catch (Exception ex)
            {
                return;
            }
        }

        private static void LoadTestData(ref List<User> users, ref List<Product> products)
        {
            User user1 = new User()
            {
                Id = 2,
                Username = "Nahuel",
                Password = "Nah123"
            };
            users.Add(user1);
            User user2 = new User()
            {
                Id = 1,
                Username = "Alan",
                Password = "Alan123"
            };
            users.Add(user2);
            User user3 = new User()
            {
                Id = 3,
                Username = "Lucas",
                Password = "Luc123"
            };
            users.Add(user3);

            Product prod1 = new Product()
            {
                Id = 1,
                OwnerUserName = user1.Username,
                Name = "Mesa",
                Description = "Primera mesa de Nahuel",
                Stock = 4,
                Price = 252,
                Image = "sin imágen"
            };
            products.Add(prod1);
            Product prod2 = new Product()
            {
                Id = 2,
                OwnerUserName = user1.Username,
                Name = "Silla",
                Description = "Primera silla de Nahuel",
                Stock = 3,
                Price = 134,
                Image = "sin imágen"
            };
            products.Add(prod2);
            Product prod3 = new Product()
            {
                Id = 3,
                OwnerUserName = user2.Username,
                Name = "Cama",
                Description = "Primera cama de Alan",
                Stock = 7,
                Price = 300,
                Image = "sin imágen"
            };
            products.Add(prod3);
            Product prod4 = new Product()
            {
                Id = 4,
                OwnerUserName = user3.Username,
                Name = "Escritorio",
                Description = "Primer escritorio de Lucas",
                Stock = 1,
                Price = 450,
                Image = "sin imágen"
            };
            products.Add(prod4);
        }

        static void ApagarServidor()
        {
            isServerOn = false;
        }
    }
}