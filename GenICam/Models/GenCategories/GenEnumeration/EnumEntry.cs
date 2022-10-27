using System.Collections.Generic;

namespace GenICam
{
    /// <summary>
    /// Enumeration entry class.
    /// </summary>
    public class EnumEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnumEntry"/> class.
        /// </summary>
        /// <param name="value">the index value.</param>
        /// <param name="isImplemented">Is implemented.</param>
        public EnumEntry(long value, IIsImplemented isImplemented = null)
        {
            Value = value;

            // Keeping on purpose, waiting for implementation.
            // IsImplemented = isImplemented;
        }

        /// <summary>
        /// Gets the index value.
        /// </summary>
        public long Value { get; private set; }

        // Keeping on purpose, waiting for implementation.
        // public IIsImplemented IsImplemented { get; private set; }
    }
}