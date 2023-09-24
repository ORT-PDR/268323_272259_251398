﻿using Common;
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
        private static object locker = new object();
        static bool servidorEncendido = true;
        
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

            var localEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 20000);
            socketServer.Bind(localEndPoint);
            socketServer.Listen(10);
            new Thread(() => HandleServer()).Start();
            Println("Esperando por clientes....");
            int cantClients = 1;
            
            while (servidorEncendido) 
            {
                
                Socket socketClient = socketServer.Accept();
                clientesActivos.Add(socketClient);
                Println("Se conecto un cliente..");
                cantClients++;
                new Thread(() => HandleClient(socketClient, cantClients, users, products)).Start();
            }

            for(int i = 0; i< clientesActivos.Count; i++)
            {
                clientesActivos[i].Shutdown(SocketShutdown.Both);
                clientesActivos[i].Close();
            }

            socketServer.Shutdown(SocketShutdown.Both);
            socketServer.Close();
        }

        static void HandleClient(Socket socketClient, int nroClient, List<User> users, List<Product> products) 
        {
            bool connected = true;
            SocketHelper socketHandler = new SocketHelper(socketClient);

            int userId = 0;
            LogIn(socketHandler, ref connected, users, ref userId);

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
                        Product product = CreateProduct(socketHandler, ref connected, userId, socketClient);
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
                            Println("Se agrego el producto");
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
                                SendData(socketHandler, "ok");
                            }
                        }
                        break;

                    case 3:
                        //Modificación de producto
                        SendClientProducts(nroClient, products, socketHandler);

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
                                    FileStreamHandler.Delete(productToModify.Name);
                                }

                                
                                Console.WriteLine("Antes de recibir el archivo nuevo");
                                var fileCommonHandler2 = new FileCommsHandler(socketClient);
                                fileCommonHandler2.ReceiveFile();
                                string productImage = productName + ".png";
                                string imageName = productImage;
                                connected = ReceiveData(socketHandler, ref productImage);
                                productToModify.Image = productImage;

                                Console.WriteLine("Archivo nuevo recibido!!");

                                break;
                        }
                        break;
                    case 4:
                        //Baja de producto
                        List<Product> clientProducts = GetClientProducts(products, nroClient);
                        SendProducts(socketHandler, clientProducts, true);
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
                        SendProducts(socketHandler, products, false);

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
                            string image = consultedProduct.Name + "InServer.png";
                            string searchDirectory = @"C:\Users\Alan\Desktop\ProgRedes\oblProg\268323_272259_251398\PrimerEjemploSocket\bin\Debug\net6.0";  // Replace with your image folder path                                                                                                                                      // Search for image files with the specified name
                            string[] imageFiles = Directory.GetFiles(searchDirectory, $"{image}.*");
                            string path = imageFiles[0];

                            fileCommonHandler.SendFile(path, consultedProduct.Name+"InClient.png");
                            
                            SendData(socketHandler, image);
                            Console.WriteLine("Se envio el archivo al Cliente");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            SendData(socketHandler, "");
                        }
                        SendData(socketHandler, consultedProduct.ToString());

                        break;

                    case 7:
                        RateAProduct(socketHandler, ref connected, ref products, userId);

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
        
        static void HandleServer()
        {
           
            while (servidorEncendido)
            {
                Console.WriteLine("Desea apagar el servidor?");
                Console.WriteLine("1. Si");
                Console.WriteLine("2. No");
                
                string input = Console.ReadLine();
                int option = 0;
                try
                {
                    int checkOption = int.Parse(input);
                    option = checkOption;
                }
                catch (Exception ex)
                {

                }
                
                switch (option)
                {
                    case 1:
                        servidorEncendido = false;
                       
                        break;
                    case 2:
                        break;
                    default:
                        Println("Opción no válida.");
                        break;
                }
            }
           
        }
        
        private static void SendClientProducts(int nroClient, List<Product> products, SocketHelper socketHelper)
        {
            List<Product> productosDelCliente;
            lock (locker)
            {
                productosDelCliente = products.Where(prod => prod.OwnerId == nroClient).ToList();
            }
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
            lock (locker)
            {
                foreach (Product product in products)
                {
                    string mensaje = product.Name + "@" + product.Stock.ToString();
                    SendData(socketHelper, mensaje);
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
                Console.WriteLine("Se recibio: " + text);
                return true;
            }
            catch (SocketException e)
            {
                return false;
            }
        }

        private static void LogIn(SocketHelper socketHelper, ref bool connected, List<User> users, ref int userId)
        {
            bool correctUser = false;
            string user = "";
            while (!correctUser)
            {
                connected = ReceiveData(socketHelper, ref user);
                correctUser = UserIsCorrect(user, users, ref userId);

                if (correctUser)
                {
                    SendData(socketHelper, "ok");
                    Println(user + " conectado");
                    correctUser = true;
                }
                else
                {
                    SendData(socketHelper, "Nombre de usuario y/o contraseña incorrecto/s. Vuelva a intentarlo.");
                }
            }
        }

        private static bool UserIsCorrect(string user, List<User> users, ref int userId)
        {
            string userName = "";
            string userPass = "";
            try
            {
                userName = user.Split('#')[0];
                userPass = user.Split('#')[1];

            }catch(Exception e)
            {
                return false;
            }


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

        private static Product CreateProduct(SocketHelper socketHelper, ref bool connected, int userId, Socket socketClient)
        {
            string productName = "";
            connected = ReceiveData(socketHelper, ref productName);

            string productDescription = "";
            connected = ReceiveData(socketHelper, ref productDescription);

            string strProductStock = "";
            connected = ReceiveData(socketHelper, ref strProductStock);
            int productStock = int.Parse(strProductStock);

            string strProductPrice = "";
            connected = ReceiveData(socketHelper, ref strProductPrice);
            int productPrice = int.Parse(strProductPrice);

            //recibir la imagen del cliente
            Console.WriteLine("Antes de recibir el archivo");
            var fileCommonHandler = new FileCommsHandler(socketClient);
            fileCommonHandler.ReceiveFile();
            string productImage = productName + ".png";
            string imageName = productImage;
            connected = ReceiveData(socketHelper, ref productImage);


            Console.WriteLine("Archivo recibido!!");

            if (connected)
            {
                Product product = new Product();
                product.Name = productName;
                product.Description = productDescription;
                product.Price = productPrice;
                product.Stock = productStock;
                product.Image = imageName;
                product.OwnerId = userId;
                return product;
            }
            return null;
        }

        private static void SendProducts(SocketHelper socketHelper, List<Product> products, bool withStock)
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
        }

        private static List<Product> GetClientProducts(List<Product> products, int nroClient)
        {
            List<Product> clientProducts;
            lock (locker)
            {
                clientProducts = products.Where(prod => prod.OwnerId == nroClient).ToList();
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
                FileStreamHandler.Delete(prodToDelete);
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

        private static void RateAProduct(SocketHelper socketHelper, ref bool connected, ref List<Product> products, int userId)
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
            review.UserId = userId;
            review.Comment = opinion;
            review.Rating = rating;

            lock (locker)
            {
                foreach (var prod in products)
                {
                    if (prod.Id == idProduct)
                    {
                        prod.Reviews.Add(review);
                        break;
                    }
                }
            }
        }

        private static void LoadTestData(ref List<User> users, ref List<Product> products)
        {
            User user1 = new User()
            {
                Id = 1,
                Username = "Nahuel",
                Password = "Nah123"
            };
            users.Add(user1);
            User user2 = new User()
            {
                Id = 2,
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
                OwnerId = user1.Id,
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
                Id = 3,
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
                Id = 4,
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