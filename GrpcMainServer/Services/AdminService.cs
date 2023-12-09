using Domain;
using DTOs;
using Grpc.Core;
//using Servidor;
using Microsoft.Extensions.Logging;
using Servidor;
using ServidorAdmin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GrpcMainServer { 
    public class AdminService : Admin.AdminBase
    {
        public override Task<MessageReply> PostProduct(ProductDTO request, ServerCallContext context)
        {
            ProgramServidor session = ProgramServidor.Instance;
            Console.WriteLine("Antes de crear el usuario con nombre {0}",request.Name);
            Product product = new()
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                Stock = request.Stock,
                Image = request.Image,
                OwnerUserName = request.OwnerUserName
            };
            Product receivedProduct = session.CreateProduct(product);
            return Task.FromResult(new MessageReply { Message = receivedProduct.ToString() });
        }

        

    }
}