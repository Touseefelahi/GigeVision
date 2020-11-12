using System;
using System.Collections.Generic;
using System.Text;

namespace GenICam
{
    public class GenBoolean : GenCategory, IGenBoolean
    {
        public bool Value { get; private set; }
        public Dictionary<string, IPRegister> Registers { get; internal set; }

        public GenBoolean(CategoryProperties categoryProperties, Dictionary<string, IPRegister> registers)
        {
            CategoryProperties = categoryProperties;
            Registers = registers;
        }

        public bool GetValue()
        {
            return Value;
        }

        public void SetVlaue(bool value)
        {
            Value = value;
        }
    }
}