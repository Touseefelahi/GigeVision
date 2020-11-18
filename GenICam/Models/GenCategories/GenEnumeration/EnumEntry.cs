using System.Collections.Generic;

namespace GenICam
{
    public class EnumEntry
    {
        public uint Value { get; private set; }
        public IIsImplemented IsImplemented { get; private set; }

        public Dictionary<string, IPValue> Expressions { get; private set; }

        public EnumEntry(uint value, IIsImplemented isImplemented)
        {
            Value = value;
            IsImplemented = isImplemented;
        }
    }
}