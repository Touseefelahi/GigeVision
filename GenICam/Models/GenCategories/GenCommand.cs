using System;
using System.Collections.Generic;

namespace GenICam
{
    public class GenCommand : GenCategory, IGenCommand
    {
        public Int64 Value { get; private set; }
        public Int64 CommandValue { get; private set; }
        public Dictionary<string, IPRegister> Registers { get; internal set; }

        public GenCommand(CategoryProperties categoryProperties, Int64 commandValue, Dictionary<string, IPRegister> registers)
        {
            Registers = registers;
            CategoryProperties = categoryProperties;
            CommandValue = commandValue;
        }

        public void Execute()
        {
            throw new NotImplementedException();
        }

        public bool IsDone()
        {
            throw new NotImplementedException();
        }
    }
}