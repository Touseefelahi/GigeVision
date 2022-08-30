using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenICam
{
    /// <summary>
    /// Maps to a drop down box
    /// </summary>
    public interface IEnumeration
    {
        /// <summary>
        /// Returns the index value corresponding to the enumeration value
        /// </summary>
        /// <returns></returns>
        Task<long> GetIntValueAsync();

        Task SetIntValueAsync(long value);
        Dictionary<string, EnumEntry> GetEntries();
        [Obsolete]
        /// <summary>
        /// returns a list of valid enumeration values
        /// </summary>
        /// <param name="entries"></param>
        Task  GetSymbolics(Dictionary<string, EnumEntry> entries);
        [Obsolete]
        EnumEntry GetEntryByName(string entryName);
        [Obsolete]
        EnumEntry GetEntry(Int64 entryValue);
        [Obsolete]
        EnumEntry GetCurrentEntry(Int64 entryValue);
    }
}