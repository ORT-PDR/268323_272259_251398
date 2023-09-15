using Common;
using Protocolo;
using Servidor;
using System.Collections.Generic;
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
                new Thread(() => HandleClient(socketClient, cantClients, users, products)).Start();
            }
        }

        static void HandleClient(Socket socketClient, int nroClient, List<User> users, List<Product> products) 
        {
            bool connected = true;
            ManejoDataSocket manejoDataSocket = new ManejoDataSocket(socketClient);

            int userId = 0;
            LogIn(manejoDataSocket, ref connected, users, ref userId);

            int option = 0;
            string strOption = "";
            while (connected)
            {
                connected = ReceiveData(manejoDataSocket, ref strOption);
                if (!connected) break;
                option = int.Parse(strOption);
                switch (option)
                {
                    case 1:
                        //Publicación de producto
                        Product product = CreateProduct(manejoDataSocket, ref connected, userId);

                        if (!products.Contains(product))
                        {
                            products.Add(product);
                            Println("Se agrego el producto");
                        }
                        break;
                    case 2:
                        //Compra de productos
                        SendProductsNameStock(manejoDataSocket, products);

                        string message = "";
                        connected = ReceiveData(manejoDataSocket, ref message);
                        if (connected)
                        {
                            string nameProduct = message.Split('@')[0];
                            string amountBought = message.Split('@')[1];
                            Product productBought = products.Find(p => p.Name == nameProduct);
                            int stock = productBought.Stock - int.Parse(amountBought);
                            if (stock < 0) SendData(manejoDataSocket, "error");
                            else
                            {
                                productBought.Stock -= int.Parse(amountBought);
                                SendData(manejoDataSocket, "ok");
                            }
                        }
                        break;

                    case 3:
                        //Modificación de producto
                        SendClientProducts(nroClient, products, manejoDataSocket);

                        string productName = "";
                        connected = ReceiveData(manejoDataSocket, ref productName);
                        Product productToModify = products.Find(p => p.Name == productName);

                        string atributeOption = "";
                        connected = ReceiveData(manejoDataSocket, ref atributeOption);

                        string newValue = "";
                        connected = ReceiveData(manejoDataSocket, ref newValue);

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
                        break;
                    case 4:
                        //Baja de producto
                        List<Product> clientProducts = GetClientProducts(products, nroClient);
                        SendProducts(manejoDataSocket, clientProducts, true);
                        DeleteProduct(manejoDataSocket, ref connected, clientProducts, ref products);

                        break;
                    case 5:
                        //Búsqueda de productos
                        string filterText = "";
                        connected = ReceiveData(manejoDataSocket, ref filterText);

                        foreach (var prod in products)
                        {
                            if (prod.Name.Contains(filterText))
                            {
                                SendData(manejoDataSocket, prod.ToString());
                                Println(prod.Name);
                            }
                        }
                        SendData(manejoDataSocket, "end");

                        break;
                    case 6:
                        //consultar un producto específico
                        SendProducts(manejoDataSocket, products, false);

                        string productToConsult = "";
                        connected = ReceiveData(manejoDataSocket, ref productToConsult);

                        Product consultedProduct = products.FirstOrDefault(prod => prod.Name == productToConsult);

                        SendData(manejoDataSocket, consultedProduct.ToString());

                        break;

                    case 7:
                        RateAProduct(manejoDataSocket, ref connected, ref products, userId);

                        break;
                    
                    case 8:
                        
                        break;

                    default: 
                        
                        break;
                }
            }
            Console.WriteLine("Cliente {0} desconectado", nroClient);
        }

        private static void SendClientProducts(int nroClient, List<Product> products, ManejoDataSocket manejoDataSocket)
        {
            List<Product> productosDelCliente = products.Where(prod => prod.OwnerId == nroClient).ToList();

            SendData(manejoDataSocket, productosDelCliente.Count.ToString());

            for (int i = 0; i < productosDelCliente.Count; i++)
            {
                string productoMostrado = productosDelCliente.ElementAt(i).Name;
                SendData(manejoDataSocket, productoMostrado);
            }
            SendData(manejoDataSocket, "end");
        }

        private static void SendProductsNameStock(ManejoDataSocket manejoDataSocket, List<Product> products)
        {
            foreach (Product product in products)
            {
                string mensaje = product.Name + "@" + product.Stock.ToString();
                SendData(manejoDataSocket, mensaje);
            }
            SendData(manejoDataSocket, "end");
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
                Console.WriteLine("Se recibio: " + text);
                return true;
            }
            catch (SocketException e)
            {
                return false;
            }
        }

        private static void LogIn(ManejoDataSocket manejoDataSocket, ref bool connected, List<User> users, ref int userId)
        {
            bool correctUser = false;
            string user = "";
            while (!correctUser)
            {
                connected = ReceiveData(manejoDataSocket, ref user);
                correctUser = UserIsCorrect(user, users, ref userId);

                if (correctUser)
                {
                    SendData(manejoDataSocket, "ok");
                    Println(user + " conectado");
                    correctUser = true;
                }
                else
                {
                    SendData(manejoDataSocket, "Nombre de usuario y/o contraseña incorrecto/s. Vuelva a intentarlo.");
                }
            }
        }

        private static bool UserIsCorrect(string user, List<User> users, ref int userId)
        {
            string userName = user.Split('#')[0];
            string userPass = user.Split('#')[1];
            
            foreach (User usr in users)
            {
                if (usr.Username == userName && usr.Password == userPass)
                {
                    userId = usr.Id;
                    return true;
                }
            }
            return false;
        }

        private static Product CreateProduct(ManejoDataSocket manejoDataSocket, ref bool connected, int userId)
        {
            string productName = "";
            connected = ReceiveData(manejoDataSocket, ref productName);

            string productDescription = "";
            connected = ReceiveData(manejoDataSocket, ref productDescription);

            string strProductStock = "";
            connected = ReceiveData(manejoDataSocket, ref strProductStock);
            int productStock = int.Parse(strProductStock);

            string strProductPrice = "";
            connected = ReceiveData(manejoDataSocket, ref strProductPrice);
            int productPrice = int.Parse(strProductPrice);

            string productImage = "";
            connected = ReceiveData(manejoDataSocket, ref productImage);

            if (connected)
            {
                Product product = new Product();
                product.Name = productName;
                product.Description = productDescription;
                product.Price = productPrice;
                product.Stock = productStock;
                product.Image = productImage;
                product.OwnerId = userId;
                return product;
            }
            return null;
        }

        private static void SendProducts(ManejoDataSocket manejoDataSocket, List<Product> products, bool withStock)
        {
            SendData(manejoDataSocket, products.Count.ToString());

            foreach (Product prod in products)
            {
                if (withStock)
                {
                    SendData(manejoDataSocket, prod.Name + " | Stock: " + prod.Stock);
                }
                else
                {
                    SendData(manejoDataSocket, prod.Name);
                }
            }
        }

        private static List<Product> GetClientProducts(List<Product> products, int nroClient)
        {
            return products.Where(prod => prod.OwnerId == nroClient).ToList();
        }

        private static void DeleteProduct(ManejoDataSocket manejoDataSocket, ref bool connected, List<Product> clientProducts, ref List<Product> products)
        {
            string prodToDelete = "";
            connected = ReceiveData(manejoDataSocket, ref prodToDelete);


            int productStock = GetProductStock(clientProducts, prodToDelete);
            if (productStock <= 0)
            {
                SendData(manejoDataSocket, "El producto seleccionado no existe");
            }
            else if (productStock == 1){
                products = clientProducts.Where(prod => !(prod.Name.Equals(prodToDelete))).ToList();
                SendData(manejoDataSocket, "Se ha eliminado el producto correctamente");
            }
            else
            {
                products.Find(prod => prod.Name == prodToDelete).Stock--;
                SendData(manejoDataSocket, "Se ha eliminado el producto correctamente");
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

        private static void RateAProduct(ManejoDataSocket manejoDataSocket, ref bool connected, ref List<Product> products, int userId)
        {
            string strIdProduct = "";
            connected = ReceiveData(manejoDataSocket, ref strIdProduct);
            int idProduct = int.Parse(strIdProduct);

            string opinion = "";
            connected = ReceiveData(manejoDataSocket, ref opinion);

            string strRating = "";
            connected = ReceiveData(manejoDataSocket, ref strRating);
            int rating = int.Parse(strRating);

            Review review = new Review();
            review.UserId = userId;
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
                OwnerId = user1.Id,
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
                OwnerId = user1.Id,
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
                OwnerId = user2.Id,
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
                OwnerId = user3.Id,
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