using System.Runtime.Serialization;

namespace Cliente
{
    [Serializable]
    public class ExitProgramException : Exception
    {
        public ExitProgramException()
        {
        }

        public ExitProgramException(string? message) : base(message)
        {
        }

        public ExitProgramException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected ExitProgramException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}