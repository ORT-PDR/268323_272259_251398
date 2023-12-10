using Common;
using Microsoft.AspNetCore.Hosting.Server;
using Servidor;
using System.Net;
using WebApiRabbitMQ.Service;

namespace ServidorCompras
{
    public class Program
    {
        private static MQService mq;

       

        public static void Main(string[] args)
        {
            // creamos conexion con RabbitMQ
            mq = new MQService();
            StartPurchaseServer(mq);

           

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }

        public static async Task StartPurchaseServer(MQService mq)
        {
            Console.WriteLine("Server will start accepting connections from the clients");
          //  await Task.Run(() => server.StartReceivingConnections());
            await Task.Run(() => mq.HandleQueue());
        }

        public static async Task ConnectToOldServer()
        {

        }

       
    }
}