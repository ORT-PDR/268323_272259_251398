using Protocolo;
using Servidor;
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
                ReceiveData(manejoDataSocket, ref strOption);
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
                        for(int i=0; i<products.Count; i++)
                        {
                            string productoMostrado = products.ElementAt(i).Name;
                            SendData(manejoDataSocket, productoMostrado);
                        }

                        SendData(manejoDataSocket, "end");

                        break;
                    case 4:
                        //Modificación de producto
                        break;
                    case 5:
                        //Baja de producto
                        List<Producto> productosDelCliente = products.Where(prod => prod.OwnerId == nroClient).ToList();

                        for (int i = 0; i < productosDelCliente.Count; i++)
                        {
                            string productoMostrado = products.ElementAt(i).Name;
                            SendData(manejoDataSocket, productoMostrado);
                        }
                        SendData(manejoDataSocket, "end");

                        string nombreProductoAEliminar = "";
                        connected = ReceiveData(manejoDataSocket, ref nombreProductoAEliminar);
                        int stockProdcto = productosDelCliente.FirstOrDefault(prod => prod.Name == nombreProductoAEliminar).Stock;
                        Console.WriteLine("stock de " + nombreProductoAEliminar + " es " + stockProdcto);
                        if (stockProdcto == 1)
                        {
                            products = products.Where(prod => !(prod.Name.Equals(nombreProductoAEliminar) && prod.OwnerId == nroClient)).ToList();
                        }
                        else
                        {
                            Producto productoAModificar = products.FirstOrDefault(prod => prod.Name == nombreProductoAEliminar && prod.OwnerId == nroClient);
                            productoAModificar.Stock--;
                        }

                        break;
                    case 6:
                        //Búsqueda de productos


                        break;
                    
                    case 7:
                        
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
            connected = ReceiveData(manejoDataSocket, ref productName);

            string strProductStock = "";
            connected = ReceiveData(manejoDataSocket, ref strProductStock);
            int productStock = int.Parse(strProductStock);

            string strProductPrice = "";
            connected = ReceiveData(manejoDataSocket, ref strProductPrice);
            int productPrice = int.Parse(strProductPrice);

            string productImage = "";
            connected = ReceiveData(manejoDataSocket, ref productImage);

            Producto product = new Producto();
            product.Name = productName;
            product.Description = productDescription;
            product.Price = productPrice;
            product.Stock = productStock;
            product.Image = productImage;
            product.OwnerId = userId;

            return product;
        }
    }
}