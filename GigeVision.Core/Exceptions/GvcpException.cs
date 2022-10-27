using System;

namespace GigeVision.Core
{
    public class GvcpException : Exception
    {
        public GvcpException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
