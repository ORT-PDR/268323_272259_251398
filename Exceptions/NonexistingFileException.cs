using System.Runtime.Serialization;

namespace Exceptions
{
    [Serializable]
    public class NonexistingFileException : Exception
    {
        public NonexistingFileException()
        {
        }

        public NonexistingFileException(string? message) : base(message)
        {
        }

        public NonexistingFileException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected NonexistingFileException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}