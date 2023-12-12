using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Domain;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MailServer.Service
{
    public class MQServiceMailServer
    {
        public MQServiceMailServer()
        {
            //HandleQueue();
        }

        public async Task HandleQueue() 
        {
            var factory = new ConnectionFactory() { HostName = "localhost" }; // Defino la conexion
            var connection = factory.CreateConnection();
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: "purchases", type: ExchangeType.Fanout); // Le indicamos un exchange tipo fanout

                var queueName = channel.QueueDeclare().QueueName; //Declaro una queue por defecto

                channel.QueueBind(queue: queueName,
                                  exchange: "purchases",
                                  routingKey: "");

                Console.WriteLine(" [*] Waiting for purchases.");

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var purchaseObject = JsonSerializer.Deserialize<Purchase>(message);

                    Console.WriteLine(" [x] Enviando correo a {0}", purchaseObject.UserName);
                    Thread.Sleep(5000);



                };
                channel.BasicConsume(queue: queueName,
                                     autoAck: true,
                                     consumer: consumer);

                Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();


            }
        }
    }
}

