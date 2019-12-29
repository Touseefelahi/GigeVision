using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigeVision.Core.Exceptions
{
    [Serializable]
    public class RegisterConversionException : Exception
    {
        public RegisterConversionException()
        {
        }

        public RegisterConversionException(string message)
            : base(message)
        {
        }

        public RegisterConversionException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected RegisterConversionException(System.Runtime.Serialization.SerializationInfo serializationInfo,
            System.Runtime.Serialization.StreamingContext streamingContext)
        {
        }
    }
}