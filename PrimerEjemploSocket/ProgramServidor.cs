using Common;
using Protocolo;
using Servidor;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace PrimerEjemploSocket
{
    public class ProgramServidor
    {
        static readonly SettingsManager settingMng = new SettingsManager();
        static void Main(string[] args)
        {
            List<Product> products = new List<Product>();
            List<User> users = new List<User>();
            LoadTestData(ref users, ref products);

            Println("Inciar Servidor...");
            var socketServer = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);

            string serverIp = settingMng.ReadSettings(ServerConfig.serverIPconfigKey);
            int serverPort = int.Parse(settingMng.ReadSettings(ServerConfig.serverPortconfigKey));

            var localEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 20000);
            socketServer.Bind(localEndPoint);
            socketServer.Listen(10);
            Println("Esperando por clientes....");
            int cantClients = 0;

            while (true) 
            {
                Socket socketClient = socketServer.Accept();
                
                
                Println("Se conecto un cliente..");
                cantClients++;
                new Thread(() => HandleClient(socketClient, cantClients, ref users, ref products)).Start();
            }
        }

        static void HandleClient(Socket socketClient, int nroClient, ref List<User> users, ref List<Product> products) 
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
                        Product product = CreateProduct(socketHandler, ref connected, products);
                        products.Add(product);
                        SendData(socketHandler, "OK");
                        Println(socketHandler.UserName + " agregó un nuevo producto.");

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
                            Product productBought = products.Find(p => p.Name == nameProduct);
                            int stock = productBought.Stock - int.Parse(amountBought);
                            if (stock < 0) SendData(socketHandler, "error");
                            else
                            {
                                productBought.Stock -= int.Parse(amountBought);
                                Println(socketHandler.UserName + " compró " + amountBought + "unidades de " + nameProduct);
                                SendData(socketHandler, "ok");
                            }
                        }
                        break;

                    case 3:
                        //Modificación de producto
                        SendClientProducts(products, socketHandler);

                        string productName = "";
                        connected = ReceiveData(socketHandler, ref productName);
                        Product productToModify = products.Find(p => p.Name == productName);

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
                                productToModify.Image = newValue;
                                break;
                        }
                        Println(socketHandler.UserName + " modificó el producto " + productName);

                        break;
                    case 4:
                        //Baja de producto
                        List<Product> clientProducts = GetClientProducts(products, socketHandler.UserName);
                        SendProducts(socketHandler, clientProducts, true);
                        DeleteProduct(socketHandler, ref connected, ref products);

                        break;
                    case 5:
                        //Búsqueda de productos
                        string filterText = "";
                        connected = ReceiveData(socketHandler, ref filterText);

                        foreach (var prod in products)
                        {
                            if (prod.Name.Contains(filterText))
                            {
                                SendData(socketHandler, prod.ToString());
                                Println(prod.Name);
                            }
                        }
                        SendData(socketHandler, "end");

                        break;
                    case 6:
                        //consultar un producto específico
                        SendProducts(socketHandler, products, false);

                        string productToConsult = "";
                        connected = ReceiveData(socketHandler, ref productToConsult);

                        Product consultedProduct = products.FirstOrDefault(prod => prod.Name == productToConsult);

                        SendData(socketHandler, consultedProduct.ToString());

                        break;

                    case 7:
                        RateAProduct(socketHandler, ref connected, ref products, socketHandler.UserName);

                        break;
                    
                    case 8:
                        
                        break;

                    default: 
                        
                        break;
                }
            }
            Console.WriteLine("Cliente {0} desconectado", nroClient);
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

        private static void SendClientProducts(List<Product> products, SocketHelper socketHelper)
        {
            List<Product> productosDelCliente = products.Where(prod => prod.OwnerUserName == socketHelper.UserName).ToList();

            SendData(socketHelper, productosDelCliente.Count.ToString());

            for (int i = 0; i < productosDelCliente.Count; i++)
            {
                string productoMostrado = productosDelCliente.ElementAt(i).Name;
                SendData(socketHelper, productoMostrado);
            }
            SendData(socketHelper, "end");
        }

        private static void SendProductsNameStock(SocketHelper socketHelper, List<Product> products)
        {
            foreach (Product product in products)
            {
                string mensaje = product.Name + "@" + product.Stock.ToString();
                SendData(socketHelper, mensaje);
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
                //Console.WriteLine("Se recibio: " + text);
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
            string userName = "";
            while (!correctUser)
            {
                connected = ReceiveData(socketHelper, ref user);
                correctUser = UserIsCorrect(user, users, ref userName);

                if (correctUser)
                {
                    SendData(socketHelper, "ok");
                    socketHelper.UserName = userName;
                    Println(socketHelper.UserName + " conectado");
                    correctUser = true;
                }
                else
                {
                    SendData(socketHelper, "Nombre de usuario y/o contraseña incorrecto/s. Vuelva a intentarlo.");
                }
            }
        }

        private static bool UserIsCorrect(string user, List<User> users, ref string userName)
        {
            string userNameReceived = user.Split('#')[0];
            string userPass = user.Split('#')[1];
            
            foreach (User usr in users)
            {
                if (usr.Username == userNameReceived && usr.Password == userPass)
                {
                    userName = usr.Username;
                    return true;
                }
            }
            return false;
        }

        private static Product CreateProduct(SocketHelper socketHelper, ref bool connected, List<Product> products)
        {
            string productName = "";
            connected = ReceiveData(socketHelper, ref productName);
            bool isInProducts = products.Exists(product => product.Name.ToLower().Equals(productName.ToLower()));
            while (connected && isInProducts)
            {
                SendData(socketHelper,"Error: Producto ya ingresado al sistema.");
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

            string productImage = "";
            connected = ReceiveData(socketHelper, ref productImage);

            if (connected)
            {
                Product product = new Product();
                product.Name = productName;
                product.Description = productDescription;
                product.Price = productPrice;
                product.Stock = productStock;
                product.Image = productImage;
                product.OwnerUserName = socketHelper.UserName;
                return product;
            }
            return null;
        }

        private static void SendProducts(SocketHelper socketHelper, List<Product> products, bool withStock)
        {
            SendData(socketHelper, products.Count.ToString());

            foreach (Product prod in products)
            {
                if (withStock)
                {
                    SendData(socketHelper, prod.Name + " | Stock: " + prod.Stock);
                }
                else
                {
                    SendData(socketHelper, prod.Name);
                }
            }
        }

        private static List<Product> GetClientProducts(List<Product> products, string clientUserName)
        {
            return products.Where(prod => prod.OwnerUserName == clientUserName).ToList();
        }

        private static void DeleteProduct(SocketHelper socketHelper, ref bool connected, ref List<Product> products)
        {
            string prodToDelete = "";
            connected = ReceiveData(socketHelper, ref prodToDelete);

            if (connected)
            {
                products = products.Where(prod => !(prod.Name.Equals(prodToDelete))).ToList();
                SendData(socketHelper, "Se ha eliminado el producto correctamente");
                FileStreamHandler.Delete(prodToDelete);
                Println(socketHelper.UserName + " eliminó el producto " + prodToDelete);
            }
        }

        private static int GetProductStock(List<Product> clientProducts, string productName)
        {
            foreach (Product prod in clientProducts)
            {
                if (prod.Name == productName)
                {
                    return prod.Stock;
                }
            }
            return 0;
        }

        private static void RateAProduct(SocketHelper socketHelper, ref bool connected, ref List<Product> products, string userName)
        {
            string strIdProduct = "";
            connected = ReceiveData(socketHelper, ref strIdProduct);
            int idProduct = int.Parse(strIdProduct);

            string opinion = "";
            connected = ReceiveData(socketHelper, ref opinion);

            string strRating = "";
            connected = ReceiveData(socketHelper, ref strRating);
            int rating = int.Parse(strRating);

            Review review = new Review();
            review.UserName = userName;
            review.Comment = opinion;
            review.Rating = rating;

            foreach (var prod in products)
            {
                if (prod.Id == idProduct)
                {
                    prod.Reviews.Add(review);
                    break;
                }
            }
            Println(socketHelper.UserName + "calificó un producto.");
        }

        private static void LoadTestData(ref List<User> users, ref List<Product> products)
        {
            User user1 = new User()
            {
                Id = 100001,
                Username = "Nahuel",
                Password = "Nah123"
            };
            users.Add(user1);
            User user2 = new User()
            {
                Id = 100002,
                Username = "Alan",
                Password = "Alan123"
            };
            users.Add(user2);
            User user3 = new User()
            {
                Id = 100003,
                Username = "Lucas",
                Password = "Luc123"
            };
            users.Add(user3);

            Product prod1 = new Product()
            {
                Id = 1001,
                OwnerUserName = user1.Username,
                Name = "Mesa",
                Description = "Primera mesa de Nahuel",
                Stock = 4,
                Price = 252,
                Image = "image"
            };
            products.Add(prod1);
            Product prod2 = new Product()
            {
                Id = 1002,
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
                Id = 1003,
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
                Id = 1004,
                OwnerUserName = user3.Username,
                Name = "Escritorio",
                Description = "Primer escritorio de Lucas",
                Stock = 1,
                Price = 450,
                Image = "image"
            };
            products.Add(prod4);
        }

    }
}