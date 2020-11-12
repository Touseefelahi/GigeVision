using System.Collections.Generic;

namespace GenICam
{
    public class EnumEntry
    {
        public uint Value { get; private set; }
        public bool IsImplemented { get; private set; }

        public Dictionary<string, IPRegister> Registers { get; private set; }

        public EnumEntry(uint value, Dictionary<string, IPRegister> registers)
        {
            Value = value;
            Registers = registers;
        }
    }
}