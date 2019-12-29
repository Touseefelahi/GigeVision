using System;

namespace GigeVision.Core.Exceptions
{
    [Serializable]
    public class InvalidIpException : Exception
    {
        public InvalidIpException()
        {
        }

        public InvalidIpException(string message)
            : base(message)
        {
        }

        public InvalidIpException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected InvalidIpException(System.Runtime.Serialization.SerializationInfo serializationInfo,
            System.Runtime.Serialization.StreamingContext streamingContext)
        {
        }
    }
}