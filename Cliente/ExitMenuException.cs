using System.Runtime.Serialization;

namespace Cliente
{
    [Serializable]
    public class ExitMenuException : Exception
    {
        public ExitMenuException()
        {
        }

        public ExitMenuException(string? message) : base(message)
        {
        }

        public ExitMenuException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected ExitMenuException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}