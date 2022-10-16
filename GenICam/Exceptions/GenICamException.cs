using System;

namespace GenICam
{
    /// <summary>
    /// Custom exception for GenICam errors.
    /// </summary>
    public class GenICamException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenICamException"/> class.
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="inner">inner exception that holds the original exception</param>
        public GenICamException(string message, Exception? inner = null)
            : base(message, inner)
        {
        }
    }
}
