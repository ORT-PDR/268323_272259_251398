
using Common.Interfaces;
using Common;
using Microsoft.AspNetCore.Hosting.Server;
using Servidor;

namespace GrpcMainServer
{
    public class Program
    {
        static readonly ISettingsManager SettingsMgr = new SettingsManager();

        public static void Main(string[] args)
        {
            var serverIpAddress = SettingsMgr.ReadSettings(ServerConfig.serverIPconfigKey);
            var serverPort = SettingsMgr.ReadSettings(ServerConfig.serverPortconfigKey);
            Console.WriteLine($"Server is starting in address {serverIpAddress} and port {serverPort}");

            ProgramServidor server = new ProgramServidor(serverIpAddress, serverPort);
            StartServer(server);

            var builder = WebApplication.CreateBuilder(args);

            // Additional configuration is required to successfully run gRPC on macOS.
            // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

            // Add services to the container.
            builder.Services.AddGrpc();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.MapGrpcService<AdminService>();
            app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

            app.Run();
        }

        public static async Task StartServer(ProgramServidor server)
        {
            Console.WriteLine("Server will start accepting connections from the clients");
            await Task.Run(() => server.StartReceivingConnections());
        }
    }
}