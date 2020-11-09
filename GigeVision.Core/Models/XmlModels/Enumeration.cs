using System;
using System.Collections.Generic;
using System.Text;

namespace GigeVision.Core.Models
{
    /// <summary>
    /// Enumeration Register
    /// </summary>
    public class Enumeration
    {
        /// <summary>
        /// Enumeration Entry List
        /// </summary>
        public Dictionary<string, uint> Entry { get; private set; }

        public object Value { get; set; }

        /// <summary>
        /// Enumeration Value Parameter
        /// </summary>
        public object ValueParameter { get; set; }

        /// <summary>
        /// Main Method
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="register"></param>
        public Enumeration(Dictionary<string, uint> entry, object pValue = null, object value = null)
        {
            Entry = entry;
            ValueParameter = pValue;
            Value = value;
            if (Value is null)
                if (ValueParameter is IntSwissKnife swissKnife)
                    Value = swissKnife.Value;
        }
    }
}