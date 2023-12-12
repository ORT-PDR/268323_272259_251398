using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ServidorAdmin.Filters
{
    public class ExceptionFilter : Attribute, IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            int statusCode;

            string errorMessage;

            if (context.Exception is Grpc.Core.RpcException e)
            {
                if (e.Status.StatusCode == StatusCode.AlreadyExists) 
                { statusCode = 400; } 
                else 
                { statusCode = 500; }
                errorMessage = e.Status.Detail;
            }
            else
            {
                statusCode = 500;
                errorMessage = context.Exception.Message;
            }

            MessageReply message = new MessageReply() { Message = errorMessage };
            context.Result = new ObjectResult(message)
            {
                StatusCode = statusCode,
            };
        }
    }
}