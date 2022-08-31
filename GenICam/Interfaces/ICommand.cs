using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenICam
{
    /// <summary>
    /// Maps to a command button
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Submits the command
        /// </summary>
        /// <returns></returns>
        Task Execute();
        /// <summary>
        /// Returns true if the command has been executed; false as long as it still  executes.
        /// </summary>
        /// <returns></returns>
        Task<bool> IsDone();
    }
}