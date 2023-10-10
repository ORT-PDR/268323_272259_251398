using Common;
using Protocolo;
using Servidor;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;

namespace PrimerEjemploSocket
{
    public class ProgramServidor
    {
        static readonly SettingsManager settingMng = new SettingsManager();
        private static object locker = new object();
        public static bool servidorEncendido = true;


        static void Main(string[] args)
        {
            List<Product> products = new List<Product>();
            List<User> users = new List<User>();
            List<Socket> clientesActivos = new List<Socket>();
            
            LoadTestData(ref users, ref products);

            Println("Inciar Servidor...");

            var socketServer = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);

            string serverIp = settingMng.ReadSettings(ServerConfig.serverIPconfigKey);
            int serverPort = int.Parse(settingMng.ReadSettings(ServerConfig.serverPortconfigKey));

            
            //var localEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 20000);

            var localEndPoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);


            socketServer.Bind(localEndPoint);
            socketServer.Listen(10);
            new Thread(() => HandleServer(socketServer)).Start();
            Println("Esperando por clientes....");


            int cantClients = 0;
            
            while (servidorEncendido) 
            {
                try
                {
                    Socket socketClient = socketServer.Accept();
                    clientesActivos.Add(socketClient);

                    Println("Se conecto un cliente..");
                    cantClients++;
                    new Thread(() => HandleClient(socketClient, cantClients, users, products)).Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Cerrando Servidor");
                }
                
                
            }

            for(int i = 0; i< clientesActivos.Count; i++)
            {
                clientesActivos[i].Shutdown(SocketShutdown.Both);
                clientesActivos[i].Close();
            }

            if (servidorEncendido)
            {
                socketServer.Shutdown(SocketShutdown.Both);
                socketServer.Close();
                Console.WriteLine("Servidor apagado");
            }
            
        }

        static void HandleClient(Socket socketClient, int nroClient, List<User> users, List<Product> products) 
        {
            bool connected = true;
            SocketHelper socketHandler = new SocketHelper(socketClient);

            string initOption = "";
            connected = ReceiveData(socketHandler, ref initOption);
            if (connected)
            {
                if (initOption == "1") LogIn(socketHandler, ref connected, users);
                else RegisterUser(socketHandler, ref connected, ref users);
            }

            int option = 0;
            string strOption = "";
            while (connected)
            {
                connected = ReceiveData(socketHandler, ref strOption);
                if (!connected) break;
                option = int.Parse(strOption);
                switch (option)
                {
                    case 1:
                        //Publicación de producto
                        Product product = CreateProduct(socketHandler, ref connected, ref products, socketClient);
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
                            SendData(socketHandler, "OK");
                            Println(socketHandler.UserName + " agrego un producto");
                        }
                        break;
                    case 2:
                        //Compra de productos
                        SendProductsNameStock(socketHandler, products);

                        string message = "";
                        connected = ReceiveData(socketHandler, ref message);
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
                            if (stock < 0) SendData(socketHandler, "error");
                            else
                            {
                                productBought.Stock -= int.Parse(amountBought);
                                Println(socketHandler.UserName + " compró " + amountBought + " unidades de " + nameProduct);
                                SendData(socketHandler, "ok");
                            }
                        }
                        break;

                    case 3:
                        //Modificación de producto
                        SendClientProducts(products, socketHandler);

                        string productName = "";
                        connected = ReceiveData(socketHandler, ref productName);
                        Product productToModify;
                        lock (locker)
                        {
                            productToModify = products.Find(p => p.Name == productName);
                        }

                        string atributeOption = "";
                        connected = ReceiveData(socketHandler, ref atributeOption);

                        string newValue = "";
                        connected = ReceiveData(socketHandler, ref newValue);

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
                                lock (locker)
                                {
                                   FileStreamHandler.Delete(productToModify.Name, ServerConfig.serverImageRouteKey);
                                }
                                
                                Console.WriteLine("Antes de recibir el archivo nuevo");
                                var fileCommonHandler2 = new FileCommsHandler(socketClient);
                                fileCommonHandler2.ReceiveFile(ServerConfig.serverImageRouteKey);
                                Console.WriteLine("llego el archivo");
                                string productImage = productName + ".png";
                                string imageName = productImage;
                                productToModify.Image = productImage;

                                Console.WriteLine("Archivo nuevo recibido!!");

                                break;
                        }
                        Println(socketHandler.UserName + " modificó el producto " + productName);
                        break;
                    case 4:
                        //Baja de producto
                        List<Product> clientProducts = GetClientProducts(products, socketHandler.UserName);
                        SendProducts(socketHandler, clientProducts);
                        DeleteProduct(socketHandler, ref connected, ref products);

                        break;
                    case 5:
                        //Búsqueda de productos
                        string filterText = "";
                        connected = ReceiveData(socketHandler, ref filterText);

                        lock (locker)
                        {
                            foreach (var prod in products)
                            {
                                if (prod.Name.Contains(filterText))
                                {
                                    SendData(socketHandler, prod.ToString());
                                    Println(prod.Name);
                                }
                            }
                        }
                        
                        SendData(socketHandler, "end");

                        break;
                    case 6:
                        //consultar un producto específico
                        SendProducts(socketHandler, products);

                        string productToConsult = "";
                        connected = ReceiveData(socketHandler, ref productToConsult);

                        Product consultedProduct;
                        lock (locker)
                        {
                            consultedProduct = products.FirstOrDefault(prod => prod.Name == productToConsult);
                        }

                        var fileCommonHandler = new FileCommsHandler(socketClient);
                        try
                        {
                            SendData(socketHandler, consultedProduct.ToString());
                            string image = consultedProduct.Name + "InServer.png";
                            string searchDirectory = @settingMng.ReadSettings(ServerConfig.serverImageRouteKey);
                            string[] imageFiles = Directory.GetFiles(searchDirectory, $"{image}.*");
                            string path = imageFiles[0];

                            fileCommonHandler.SendFile(path, consultedProduct.Name + "InClient.png");
                            SendData(socketHandler, image);

                            Console.WriteLine("Se envio el archivo al Cliente");

                        }
                        catch (System.IndexOutOfRangeException ex)
                        {
                            Console.WriteLine("Image Not Found in Server");
                            string image = "error-404";
                            string searchDirectory = @settingMng.ReadSettings(ServerConfig.serverImageRouteKey);
                            string[] imageFiles = Directory.GetFiles(searchDirectory, $"{image}.*");
                            string path = imageFiles[0];
                            fileCommonHandler.SendFile(path, "error");
                            SendData(socketHandler, "");
                        }

                        break;

                    case 7:
                        SendProducts(socketHandler, products);
                        RateAProduct(socketHandler, ref connected, ref products);

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
        
        static void HandleServer(Socket socketServer)
        {

            Console.WriteLine("Ingrese exit para cerrar el Server");
            string entrada = Console.ReadLine();
            if (entrada.Equals("exit"))
            {

                ApagarServidor();
                try
                {
                    socketServer.Shutdown(SocketShutdown.Both);
                }catch (Exception ex)
                {
                    Console.WriteLine("Apagando srvidor");
                }
                
                socketServer.Close();
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

        private static void SendData(SocketHelper socketHelper, string text)
        {
            byte[] data = Encoding.UTF8.GetBytes(text);
            byte[] dataLength = BitConverter.GetBytes(data.Length);

            if (servidorEncendido)
            {
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

        private static void LogIn(SocketHelper socketHelper, ref bool connected, List<User> users)
        {
            bool correctUser = false;
            string user = "";
            while (!correctUser && servidorEncendido)
            {
                connected = ReceiveData(socketHelper, ref user);
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

        private static Product CreateProduct(SocketHelper socketHelper, ref bool connected, ref List<Product> products, Socket socketClient)
        {
            try
            {
                //string sincronizacion = "";
                //ReceiveData(socketHelper, ref sincronizacion);
                string productName = "";
                connected = ReceiveData(socketHelper, ref productName);
                bool isInProducts = products.Exists(product => product.Name.ToLower().Equals(productName.ToLower()));
                while (connected && isInProducts)
                {
                    SendData(socketHelper, "Error: Producto ya ingresado al sistema.");
                    connected = ReceiveData(socketHelper, ref productName);
                    isInProducts = products.Exists(product => product.Name.ToLower().Equals(productName.ToLower()));

                }
                SendData(socketHelper, "OK");

                string productDescription = "";
                connected = ReceiveData(socketHelper, ref productDescription);

                string strProductStock = "";
                connected = ReceiveData(socketHelper, ref strProductStock);
                int productStock = int.Parse(strProductStock);

                string strProductPrice = "";
                connected = ReceiveData(socketHelper, ref strProductPrice);
                int productPrice = int.Parse(strProductPrice);

                string strHasImage = "";
                connected = ReceiveData(socketHelper, ref strHasImage);
                int hasImage = int.Parse(strHasImage);
                string productImage = "";
                string imageName = "";
                //recibir la imagen del cliente
                if (hasImage == 1)
                {
                    Console.WriteLine("Antes de recibir el archivo");
                    var fileCommonHandler = new FileCommsHandler(socketClient);
                    fileCommonHandler.ReceiveFile(ServerConfig.serverImageRouteKey);
                    productImage = productName + ".png";
                    imageName = productImage;
                    connected = ReceiveData(socketHelper, ref productImage);


                    Console.WriteLine("Archivo recibido!!");
                }
                else
                {
                    productImage = "sin imágen";
                    imageName = productImage;
                    connected = ReceiveData(socketHelper, ref productImage);
                    Console.WriteLine();
                }
                

                if (connected)
                {
                    Product product = new Product();
                    product.Name = productName;
                    product.Description = productDescription;
                    product.Price = productPrice;
                    product.Stock = productStock;
                    product.Image = imageName;
                    product.OwnerUserName = socketHelper.UserName;
                    return product;
                }
                return null;
            }catch(Exception ex)
            {
                return null;
            }
        }

        private static void RegisterUser(SocketHelper socketHandler, ref bool connected, ref List<User> users)
        {
            string user = "";
            connected = ReceiveData(socketHandler, ref user);

            if (connected)
            {
                while (connected && users.Exists(u => u.Username == user.Split("@")[0]))
                {
                    SendData(socketHandler, "Error: Username ya en sistema.");
                    connected = ReceiveData(socketHandler, ref user);
                }
                if (connected)
                {
                    User newUser = new User()
                    {
                        Id = users.Count + 1,
                        Username = user.Split("@")[0],
                        Password = user.Split("@")[1]
                    };
                    users.Add(newUser);
                    socketHandler.UserName = newUser.Username;
                    SendData(socketHandler, "OK");
                    Println(socketHandler.UserName + " se ha registrado e iniciado sesion.");
                }
            }
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
                    if (prod.Stock>0)
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

        private static void DeleteProduct(SocketHelper socketHelper, ref bool connected, ref List<Product> products)
        {
            string prodToDelete = "";
            connected = ReceiveData(socketHelper, ref prodToDelete);

            if (connected)
            {
                lock (locker)
                {
                    products = products.Where(prod => !(prod.Name.Equals(prodToDelete))).ToList();
                }
                SendData(socketHelper, "Se ha eliminado el producto correctamente");
                FileStreamHandler.Delete(prodToDelete, ServerConfig.serverImageRouteKey);
                Println(socketHelper.UserName + " eliminó el producto " + prodToDelete);
            }
        }

        private static void RateAProduct(SocketHelper socketHelper, ref bool connected, ref List<Product> products)
        {
            try
            {
                string strProductName = "";
                connected = ReceiveData(socketHelper, ref strProductName);
                // int idProduct = int.Parse(strIdProduct);

                string opinion = "";
                connected = ReceiveData(socketHelper, ref opinion);

                string strRating = "";
                connected = ReceiveData(socketHelper, ref strRating);
                int rating = int.Parse(strRating);

                Review review = new Review()
                {
                    UserName = socketHelper.UserName,
                    Comment = opinion,
                    //AmountOfRatings++,
                    //TotalRatingSum += rating,
                    //Rating = review.TotalRatingSum/review.AmountOfRatings,
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
                Image = "logo-og.png"
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
                Image = "image"
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
                Image = "image"
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
                Image = "image"
            };
            products.Add(prod4);
        }

        static void ApagarServidor()
        {
            servidorEncendido = false;

        }

    }
}