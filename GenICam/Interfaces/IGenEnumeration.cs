using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenICam
{
    public interface IGenEnumeration
    {
        Task<Int64> GetIntValueAsync();

        void SetIntValue(Int64 value);

        Dictionary<string, EnumEntry> GetEntries();

        //ToDo: Look this method up
        void GetSymbolics(Dictionary<string, EnumEntry> entries);

        EnumEntry GetEntryByName(string entryName);

        EnumEntry GetEntry(Int64 entryValue);

        //ToDo: Look this method up
        EnumEntry GetCurrentEntry(Int64 entryValue);
    }
}