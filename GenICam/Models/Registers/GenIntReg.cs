using GenICam.Models;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace GenICam
{
    /// <summary>
    /// Extracts an integer lying byte-bounded in a register.
    /// </summary>
    public class GenIntReg : RegisterBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenIntReg"/> class.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="length">The length.</param>
        /// <param name="accessMode">The access mode.</param>
        /// <param name="expressions">The expressions.</param>
        /// <param name="pAddress">The pointer in the address.</param>
        /// <param name="genPort">The GenICam port.</param>
        public GenIntReg(long? address, long length, GenAccessMode accessMode, Dictionary<string, IMathematical> expressions, object pAddress, IPort genPort)
                   : base(address, length, accessMode, pAddress, genPort)
        {
            Expressions = expressions;
        }

        /// <summary>
        /// Gets or sets the list of expressions.
        /// </summary>
        public Dictionary<string, IMathematical> Expressions { get; set; }
    }
}