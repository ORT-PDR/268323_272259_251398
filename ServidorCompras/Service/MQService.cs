using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Domain;
using Microsoft.AspNetCore.Connections;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Servidor;
using ServidorCompras;
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

                var queueName = channel.QueueDeclare().QueueName; //Declaro una queue por defecto

                channel.QueueBind(queue: queueName,
                                  exchange: "purchases",
                                  routingKey: "");

                Console.WriteLine(" [*] Esperando por compras.");

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine(" [x] {0}", message);
                    var purchaseObject = JsonSerializer.Deserialize<Purchase>(message);

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

/*
                      var exit = false;
                      while (!exit)
                      {
                          Console.WriteLine(" Ingrese un mensaje....");
                          var message = Console.ReadLine();

                          if (message.ToLower().Equals("exit"))
                          {
                              exit = true;

                          }
                          else
                          {
                              var body = Encoding.UTF8.GetBytes(message);

                              channel.BasicPublish(exchange: "purchases",
                                                   routingKey: "",
                                                   basicProperties: null,
                                                   body: body);

                              Console.WriteLine(" [x] Mensaje Enviado: {0}", message);

                          }

                      }

                      //Defino el mecanismo de consumo
                      var consumer = new EventingBasicConsumer(channel);
                      //Defino el evento que sera invocado cuando llegue un mensaje 
                      consumer.Received += (model, ea) =>
                      {
                          var body = ea.Body.ToArray();
                          var message = Encoding.UTF8.GetString(body);
                          Console.WriteLine(" [x] Received {0}", message);
                          Purchase purchase = JsonSerializer.Deserialize<Purchase>(message);


                          var servidor = ProgramServidor.Instance;
                          servidor.AddPurchase(purchase);
                      };

                      //"PRENDO" el consumo de mensajes
                      channel.BasicConsume(queue: "purchases",
                          autoAck: true,
                          consumer: consumer);

                      */