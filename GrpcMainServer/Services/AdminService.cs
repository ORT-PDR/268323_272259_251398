using Grpc.Core;
//using Servidor;
using Microsoft.Extensions.Logging;
using Servidor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GrpcMainServer { 
    public class AdminService : Admin.AdminBase
    {
        public override Task<MessageReply> PostUser(UserDTO request, ServerCallContext context)
        {
            ProgramServidor session = ProgramServidor.Instance;
            Console.WriteLine("Antes de crear el usuario con nombre {0}",request.Name);
            string message = session.CreateUser(request.Name);
            return Task.FromResult(new MessageReply { Message = message });
        }

        public override Task<MessageReply> DeleteUser(Id request, ServerCallContext context)
        {
            BusinessLogic session = BusinessLogic.GetInstance();
            bool couldDelete = session.DeleteUser(request.Id_);
            string message = couldDelete ? "Usuario eliminado correctamente" : "No se pudo eliminar usuario";
            return Task.FromResult(new MessageReply { Message = message });
        }

    }
}