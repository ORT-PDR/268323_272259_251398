using System.Text;
using System.Text.Json;
using Domain;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WebApiRabbitMQ.Data;

namespace WebApiRabbitMQ.Service
{
    public class MQService
    {
        public MQService()
        {
           
        }

        public async Task HandleQueue() 
        {
            var factory = new ConnectionFactory() { HostName = "localhost" }; // Defino la conexion
            var connection = factory.CreateConnection();
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: "purchases", type: ExchangeType.Fanout); // Le indicamos un exchange tipo fanout

                //var queueName = channel.QueueDeclare().QueueName; //Declaro una queue por defecto
                var queueName = "MensajesServidorCompras";
                channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
                //channel.QueueBind(queue: queueName, exchange: "purchases", routingKey: "");
                
                channel.QueueBind(queue: queueName,
                                  exchange: "purchases",
                                  routingKey: "");

                Console.WriteLine(" [*] Esperando por compras.");

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var purchaseObject = JsonSerializer.Deserialize<Purchase>(message);

                    var outPutMessage = "Se recibio compra de " + purchaseObject.UserName + " de " + purchaseObject.Amount + " unidades del producto " + purchaseObject.Product + " con un total de " + purchaseObject.TotalPrice + "$" + " del dia " + purchaseObject.PurchaseDate;
                    Console.WriteLine(" [x] {0}", outPutMessage);

                    var data = PurchaseDataAccess.GetInstance();
                    data.AddPurchase(purchaseObject);
                };
                channel.BasicConsume(queue: queueName,
                                     autoAck: true,
                                     consumer: consumer);

                Console.WriteLine(" Presione [enter] para salir.");
                Console.ReadLine();


            }
        }
    }
}