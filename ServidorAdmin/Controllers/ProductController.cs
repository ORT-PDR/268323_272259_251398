﻿using Microsoft.AspNetCore.Mvc;
using Grpc.Net.Client;
using Common;
using ServidorAdmin.Filters;
using System.Net;

namespace ServidorAdmin.Controllers
{
    [Route("admin/products")]
    [ApiController]
    [ExceptionFilter]
    public class ProductController : ControllerBase
    {
        private Admin.AdminClient client;
        private string _serverAddress; //= "http://localhost:5156";
        static readonly SettingsManager SettingsMgr = new SettingsManager();
        public ProductController()
        {
            AppContext.SetSwitch(
                  "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            _serverAddress = SettingsMgr.ReadSettings(AdminAPIConfig.gRCPAddressKey);
        }

        [HttpPost]
        public async Task<ActionResult> PostProduct([FromBody] ProductDTO product)
        {
            /*http://localhost:5156*/
            using var channel = GrpcChannel.ForAddress(_serverAddress);
            client = new Admin.AdminClient(channel);
            var reply = await client.PostProductAsync(product);
            return Ok(reply.Message);
        }


        [HttpGet("{name}")]
        public async Task<ActionResult> GetAllProducts([FromRoute] string name)
        {
            /*http://localhost:5156*/
            using var channel = GrpcChannel.ForAddress(_serverAddress);
            client = new Admin.AdminClient(channel);
            Name nameToSend = new Name { Name_ = name };
            var reply = await client.GetAllProductsAsync(nameToSend);
            var listOfStrings = reply.Message;

            return Ok(listOfStrings);
        }

        [HttpDelete]
        public async Task<ActionResult> DeleteProduct([FromBody] DeleteProductRequest product)
        {
            /*http://localhost:5156*/
            using var channel = GrpcChannel.ForAddress(_serverAddress);
            client = new Admin.AdminClient(channel);
            var reply = await client.DeleteProductAsync(product);
            return Ok(reply.Message);
        }

        [HttpPut]
        public async Task<ActionResult> ModifyProduct([FromBody] ModifyProductRequest modifyProductRequest)
        {
            /*http://localhost:5156*/
            using var channel = GrpcChannel.ForAddress(_serverAddress);
            client = new Admin.AdminClient(channel);
            var reply = await client.ModifyProductAsync(modifyProductRequest);
            return Ok(reply.Message);
        }

        [HttpGet("{name}/reviews")]
        public async Task<ActionResult> GetReviews([FromRoute] string name)
        {
            /*http://localhost:5156*/
            using var channel = GrpcChannel.ForAddress(_serverAddress);
            client = new Admin.AdminClient(channel);
            Name productName = new Name { Name_ = name };
            var reply = await client.GetReviewsAsync(productName);
            return Ok(reply.Message);
        }

        /*
        [HttpPost("/admin/purchases")]
        public async Task<ActionResult> BuyProduct([FromBody] PurchaseRequest purchase)
        {
            using var channel = GrpcChannel.ForAddress(_serverAddress);
            client = new Admin.AdminClient(channel);

            var reply = await client.BuyProductAsync(purchase);
            return Ok(reply.Message);
        }
        */
    }
}
