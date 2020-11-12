using System;
using System.Collections.Generic;
using System.Linq;

namespace GenICam
{
    public class GenEnumeration : GenCategory, IGenEnumeration
    {
        /// <summary>
        /// Enumeration Entry List
        /// </summary>
        public Dictionary<string, EnumEntry> Entries { get; private set; }

        /// <summary>
        /// Enumeration Value Parameter
        /// </summary>
        public Int64 Value { get; private set; }

        public Dictionary<string, IPRegister> Registers { get; internal set; }

        public GenEnumeration(CategoryProperties categoryProperties, Dictionary<string, EnumEntry> entries,
            Dictionary<string, IPRegister> registers = null)
        {
            CategoryProperties = categoryProperties;
            Entries = entries;
            Registers = registers;
        }

        public long GetIntValue()
        {
            return Value;
        }

        public void SetIntValue(long value)
        {
            Value = value;
        }

        public Dictionary<string, EnumEntry> GetEntries()
        {
            return Entries;
        }

        public void GetSymbolics(Dictionary<string, EnumEntry> list)
        {
            Entries = list;
        }

        public EnumEntry GetEntryByName(string entryName)
        {
            return Entries[entryName];
        }

        public EnumEntry GetEntry(long entryValue)
        {
            var entries = Entries.Where(x => x.Value.Value == entryValue);

            if (entries.Count() > 0)
                return entries.First().Value;

            return null;
        }

        public EnumEntry GetCurrentEntry(long entryValue)
        {
            throw new System.NotImplementedException();
        }
    }
}