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

namespace ServidorAdmin.Controllers
{
    [Route("admin/products")]
    [ApiController]
    [ExceptionFilter]
    public class ProductController : ControllerBase
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

        [HttpDelete]
        public async Task<ActionResult> DeleteProduct([FromBody] DeleteProductRequest product)
        {
            /*http://localhost:5156*/
            using var channel = GrpcChannel.ForAddress("http://localhost:5156");
            client = new Admin.AdminClient(channel);
            var reply = await client.DeleteProductsAsync(product);
            return Ok(reply.Message);
        }

        [HttpPut]
        public async Task<ActionResult> ModifyProduct([FromBody] ModifyProductRequest modifyProductRequest)
        {
            /*http://localhost:5156*/
            using var channel = GrpcChannel.ForAddress("http://localhost:5156");
            client = new Admin.AdminClient(channel);
            var reply = await client.ModifyProductAsync(modifyProductRequest);
            return Ok(reply.Message);
        }
    }
}
