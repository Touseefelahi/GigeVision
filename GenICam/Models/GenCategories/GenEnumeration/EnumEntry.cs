using System.Collections.Generic;

namespace GenICam
{
    public class EnumEntry
    {
        public uint Value { get; private set; }
        //public IIsImplemented IsImplemented { get; private set; }
        public EnumEntry(uint value, IIsImplemented isImplemented = null)
        {
            Value = value;
           // IsImplemented = isImplemented;
        }
    }
}