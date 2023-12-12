
using Common.Interfaces;
using Common;
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
            Console.WriteLine($"Servidor principal esta iniciando en la direcci�n {serverIpAddress} y puerto {serverPort}");

            ProgramServidor server = new ProgramServidor(serverIpAddress, serverPort);
            ProgramServidor.SetInstance(server);
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
            Console.WriteLine("Servidor principal empezar� a aceptar conexiones de clientes");
            await Task.Run(() => server.StartReceivingConnections());
        }
    }
}