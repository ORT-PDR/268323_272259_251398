using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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

            var factory = new ConnectionFactory() { HostName = "localhost" }; // Defino la conexion
            var connection = factory.CreateConnection();
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: "purchases", type: ExchangeType.Fanout); // Le indicamos un exchange tipo fanout

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
                    Compra compra = JsonSerializer.Deserialize<Compra>(message);

                    var data = ProgramServidor.GetInstance();
                    data.AddForecast(forecast);
                };

                //"PRENDO" el consumo de mensajes
                channel.BasicConsume(queue: "weather",
                    autoAck: true,
                    consumer: consumer);


            }

        }


        }

    }
}