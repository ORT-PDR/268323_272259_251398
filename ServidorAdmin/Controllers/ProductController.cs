using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Domain;
using Servidor;
using Grpc.Net.Client;

using System;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc;
using Common;

namespace ServidorAdmin.Controllers
{
    [Route("api/products")]
    [ApiController]
    public class ProductController : Controller
    {
        private Admin.AdminClient client;
        //  static readonly ISettingsManager SettingsMgr = new SettingsManager();
        public ProductController()
        {
            AppContext.SetSwitch(
                  "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }

        [HttpPost]
        public async Task<ActionResult> PostProduct([FromBody] ProductDTO product)
        {
            /*http://localhost:5156*/
            using var channel = GrpcChannel.ForAddress("http://localhost:5156");
            client = new Admin.AdminClient(channel);
            if (product.Image == "") product.Image = "sin imagen";
            var reply = await client.PostProductAsync(product);
            return Ok(reply.Message);
        }


        //[HttpGet("{name}")]
        //public async Task<ActionResult> GetAllProducts([FromRoute] Name name)
        //{
        //    /*http://localhost:5156*/
        //    using var channel = GrpcChannel.ForAddress("http://localhost:5156");
        //    client = new Admin.AdminClient(channel);
        //    var reply = await client.GetAllProductsAsync(name);
        //    var listOfStrings = reply.Message;

        //    return Ok(listOfStrings);
        //}

        [HttpDelete("{name}")]
        public async Task<ActionResult> DeleteProduct([FromRoute] DeleteProductRequest product)
        {
            /*http://localhost:5156*/
            using var channel = GrpcChannel.ForAddress("http://localhost:5156");
            client = new Admin.AdminClient(channel);
            var reply = await client.DeleteProductsAsync(product);
            return Ok(reply.Message);
        }
    }
}
