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
            Console.WriteLine("Antes de crear el producto con nombre {0}",request.Name);
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

        public override Task<MessageReply> GetAllProducts(Name name, ServerCallContext context)
        {
            ProgramServidor session = ProgramServidor.Instance;
            Console.WriteLine("Antes de enviar todos los productos");
           
            List<Product> receivedProduct = ProgramServidor.GetClientProducts("Alan");
            string products = "";
            foreach (Product product in receivedProduct)
            {
                products+=(product.ToString());
            }
            return Task.FromResult(new MessageReply { Message = products });
        }

        public override Task<MessageReply> DeleteProduct(DeleteProductRequest product, ServerCallContext context)
        {
            ProgramServidor session = ProgramServidor.Instance;
            Console.WriteLine("Antes de eliminar producto ", product.Name);

            try
            {
                ProgramServidor.DeleteProduct(product.UserName, product.Name);
            }
            catch (ArgumentException ex)
            {
                return Task.FromResult(new MessageReply { Message = ex.Message });
            }
            string message = "Se eliminó correctamente el producto " + product.Name;
            return Task.FromResult(new MessageReply { Message = message });
        }


    }
}