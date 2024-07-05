﻿namespace GenICam
{
    /// <summary>
    /// Interface for mathematical.
    /// </summary>
    public interface IMathematical : IPValue
    {
        /// <summary>
        /// Gets the value.
        /// </summary>
        double Value { get; }
    }
}