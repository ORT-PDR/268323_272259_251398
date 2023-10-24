using System.Runtime.Serialization;

namespace Exceptions
{
    [Serializable]
    public class ShutServerDownException : Exception
    {
        public ShutServerDownException()
        {
        }

        public ShutServerDownException(string? message) : base(message)
        {
        }

        public ShutServerDownException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected ShutServerDownException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}