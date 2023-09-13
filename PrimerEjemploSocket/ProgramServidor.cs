using Protocolo;
using Servidor;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace PrimerEjemploSocket
{
    internal class ProgramServidor
    {
        static void Main(string[] args)
        {
            List<Producto> products = new List<Producto>();
            List<Usuario> users = new List<Usuario>();

            Println("Inciar Servidor...");
            var socketServer = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
            var endpointLocal = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 20000);
            socketServer.Bind(endpointLocal);
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

        static void HandleClient(Socket socketClient, int nroClient, List<Usuario> users, List<Producto> products) 
        {
            bool connected = true;
            ManejoDataSocket manejoDataSocket = new ManejoDataSocket(socketClient);

            Usuario user = new Usuario();
            user.Id = nroClient;
            string userName = "";
            connected = ReceiveData(manejoDataSocket, ref userName);


            string pswd = "";
            connected = ReceiveData(manejoDataSocket, ref pswd);

            Println(userName + " Conectado ");

            int option = 0;

            while (connected)
            {
                //Recibe la opción seleccionada por el usuario
                string strOption = "";
                connected = ReceiveData(manejoDataSocket, ref strOption);
                if (!connected) break;
                option = int.Parse(strOption);

                Println("Seleccionada la opción " + option);
                
                switch (option)
                {
                    case 1:
                        //DESCONECTARSE
                        break;
                    case 2:
                        //Publicación de producto
                        Producto product = CreateProduct(manejoDataSocket, ref connected, user.Id);

                        if (!products.Contains(product))
                        {
                            products.Add(product);
                            Println("Se agrego el producto");
                        }
                        Print("esperando");

                        break;

                    case 3:
                        //Compra de productos
                        SendProductsNameStock(manejoDataSocket, products);

                        string message = "";
                        connected = ReceiveData(manejoDataSocket, ref message);
                        if (connected)
                        {
                            string nameProduct = message.Split('@')[0];
                            string amountBought = message.Split('@')[1];
                            Producto productBought = products.Find(p => p.Name == nameProduct);
                            int stock = productBought.Stock - int.Parse(amountBought);
                            if (stock < 0) SendData(manejoDataSocket, "error");
                            else
                            {
                                productBought.Stock -= int.Parse(amountBought);
                                SendData(manejoDataSocket, "ok");
                            }
                            
                        }
                        break;

                    case 4:
                        //Modificación de producto
                        SendClientProducts(nroClient, products, manejoDataSocket);

                        string productName = "";
                        connected = ReceiveData(manejoDataSocket, ref productName);
                        Producto productToModify = products.Find(p => p.Name == productName);

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

                    case 5:
                        byte[] data;
                        byte[] largoData;

                        List<Producto> clientProducts = GetClientProducts(products, nroClient);
                        SendProducts(manejoDataSocket, clientProducts, true);
                        DeleteProduct(manejoDataSocket, ref connected, clientProducts, ref products);

                        break;

                    case 6:
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
                    
                    case 7:
                        //consultar un producto específico
                        SendProducts(manejoDataSocket, products, false);

                        string productToConsult = "";
                        connected = ReceiveData(manejoDataSocket, ref productToConsult);

                        Producto consultedProduct = products.FirstOrDefault(prod => prod.Name == productToConsult);

                        SendData(manejoDataSocket, consultedProduct.ToString());

                        break;

                    case 8:
                        //Calificar un producto
                        string strIdProduct = "";
                        connected = ReceiveData(manejoDataSocket, ref strIdProduct);
                        int idProduct = int.Parse(strIdProduct);

                        string opinion = "";
                        connected = ReceiveData(manejoDataSocket, ref opinion);

                        string strRating = "";
                        connected = ReceiveData(manejoDataSocket, ref strRating);
                        int rating = int.Parse(strRating);

                        Reseña review = new Reseña();
                        review.UserId = user.Id;
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

                        break;
                    
                    case 9:
                        
                        break;

                    default: 
                        
                        break;
                }
            }
            Console.WriteLine("Cliente {0} desconectado", nroClient);
        }

        private static void SendClientProducts(int nroClient, List<Producto> products, ManejoDataSocket manejoDataSocket)
        {
            List<Producto> productosDelCliente = products.Where(prod => prod.OwnerId == nroClient).ToList();

            SendData(manejoDataSocket, productosDelCliente.Count.ToString());

            for (int i = 0; i < productosDelCliente.Count; i++)
            {
                string productoMostrado = productosDelCliente.ElementAt(i).Name;
                SendData(manejoDataSocket, productoMostrado);
            }
            SendData(manejoDataSocket, "end");
        }

        private static void SendProductsNameStock(ManejoDataSocket manejoDataSocket, List<Producto> products)
        {
            foreach (Producto product in products)
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

        private static Producto CreateProduct(ManejoDataSocket manejoDataSocket, ref bool connected, int userId)
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
                Producto product = new Producto();
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

        private static void SendProducts(ManejoDataSocket manejoDataSocket, List<Producto> products, bool withStock)
        {
            SendData(manejoDataSocket, products.Count.ToString());

            foreach (Producto prod in products)
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

        private static List<Producto> GetClientProducts(List<Producto> products, int nroClient)
        {
            return products.Where(prod => prod.OwnerId == nroClient).ToList();
        }

        private static void DeleteProduct(ManejoDataSocket manejoDataSocket, ref bool connected, List<Producto> clientProducts, ref List<Producto> products)
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

        private static int GetProductStock(List<Producto> clientProducts, string productName)
        {
            foreach (Producto prod in clientProducts)
            {
                if (prod.Name == productName)
                {
                    return prod.Stock;
                }
            }
            return 0;
        }
    }
}