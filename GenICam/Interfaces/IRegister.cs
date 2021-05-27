using System;

namespace GenICam
{
    public interface IRegister : IPValue, IGenRegister
    {
        /// <summary>
        /// Register Address in hex format
        /// </summary>
        Int64? Address { get; }

        /// <summary>
        /// Register Length
        /// </summary>
        Int64 Length { get; }

        /// <summary>
        /// Register Access Mode
        /// </summary>
        GenAccessMode AccessMode { get; }
    }
}