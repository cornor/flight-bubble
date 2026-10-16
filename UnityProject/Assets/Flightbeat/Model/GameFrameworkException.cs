using System;
using System.Runtime.Serialization;

namespace TEngine
{
    // Source f54917..f54920 only forward to the corresponding Exception
    // constructors. Retain the source name because default Message includes it.
    [Serializable]
    public class GameFrameworkException : Exception
    {
        public GameFrameworkException() : base() { }
        public GameFrameworkException(string message) : base(message) { }
        public GameFrameworkException(string message, Exception innerException) : base(message, innerException) { }
        protected GameFrameworkException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
