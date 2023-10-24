using System.Runtime.Serialization;

namespace Servidor
{
    [Serializable]
    public class ExitException : Exception
    {
        public ExitException()
        {
        }

        public ExitException(string? message) : base(message)
        {
        }

        public ExitException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected ExitException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}