using System;

namespace GigeVision.Core
{
    public class GvspException : Exception
    {
        public GvspException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
