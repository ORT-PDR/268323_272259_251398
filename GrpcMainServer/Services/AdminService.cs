using Domain;
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
            Product product = toEntity(request);
            try
            {
                Product receivedProduct = session.CreateProduct(product);
                return Task.FromResult(new MessageReply { Message = receivedProduct.ToString() });
            }
            catch (ArgumentException ex)
            {
                throw new RpcException(new Status(StatusCode.AlreadyExists, ex.Message));
            }    
        }

        public override Task<MessageReply> GetAllProducts(Name name, ServerCallContext context)
        {
            ProgramServidor session = ProgramServidor.Instance;
            Console.WriteLine("Antes de enviar todos los productos");
           
            List<Product> receivedProduct = session.GetClientProducts("Alan");
            string products = "";
            foreach (Product product in receivedProduct)
            {
                products+=(product.ToString());
            }
            return Task.FromResult(new MessageReply { Message = products });
        }

        public override Task<MessageReply> ModifyProduct(ModifyProductRequest requestProduct, ServerCallContext context)
        {
            ProgramServidor session = ProgramServidor.Instance;
            Console.WriteLine("Antes de modificar el producto ", requestProduct.Product.Name);

            Product product = toEntity(requestProduct.Product);
            try
            {
                session.ModifyProduct(product, requestProduct.Username);
            }
            catch (ArgumentException ex)
            {
                throw new RpcException(new Status(StatusCode.AlreadyExists, ex.Message));
            }
            string message = "Se modificó correctamente el producto " + requestProduct.Product.Name;
            return Task.FromResult(new MessageReply { Message = message });
        }

        public override Task<MessageReply> DeleteProduct(DeleteProductRequest product, ServerCallContext context)
        {
            ProgramServidor session = ProgramServidor.Instance;
            Console.WriteLine("Antes de eliminar producto ", product.Name);

            try
            {
                session.DeleteProduct(product.UserName, product.Name);
            }
            catch (ArgumentException ex)
            {
                throw new RpcException(new Status(StatusCode.AlreadyExists, ex.Message));
            }
            string message = "Se eliminó correctamente el producto " + product.Name;
            return Task.FromResult(new MessageReply { Message = message });
        }

        private Product toEntity(ProductDTO product)
        {
            return new Product
            {
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                Image = product.Image,
                OwnerUserName = product.OwnerUserName
            };
        }
    }
}