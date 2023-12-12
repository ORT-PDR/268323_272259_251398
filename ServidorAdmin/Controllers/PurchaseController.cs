using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Domain;
using Servidor;
using Grpc.Net.Client;
using Grpc.Core;
using System;
using System.Threading.Tasks;
using Common;
using ServidorAdmin.Filters;
using ServidorAdmin;

namespace WebApiRabbitMQ.Controllers
{
    [Route("admin/purchases")]
    [ApiController]
    [ExceptionFilter]
    public class PurchaseController : ControllerBase
    {

        private Admin.AdminClient client;
        private readonly string _serverAddress = "http://localhost:5156";
        //  static readonly ISettingsManager SettingsMgr = new SettingsManager();
        public PurchaseController()
        {
            AppContext.SetSwitch(
                  "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }


        [HttpPost]
        public async Task<ActionResult> BuyProduct([FromBody] PurchaseRequest purchase)
        {
            /*http://localhost:5156*/
            using var channel = GrpcChannel.ForAddress(_serverAddress);
            client = new Admin.AdminClient(channel);

            var reply = await client.BuyProductAsync(purchase);
            return Ok(reply.Message);
        }

    }
}
