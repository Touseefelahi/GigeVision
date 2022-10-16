using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenICam
{
    /// <summary>
    /// Maps to a drop down box.
    /// </summary>
    public interface IEnumeration
    {
        /// <summary>
        /// Gets the index value corresponding to the enumeration value.
        /// </summary>
        /// <returns>The index value as a long.</returns>
        public Task<long> GetValueAsync();

        /// <summary>
        /// Sets the index value corresponding to the enumeration value.
        /// </summary>
        /// <param name="value">The index value to set.</param>
        /// <returns>A task.</returns>
        public Task<IReplyPacket> SetValueAsync(long value);

        /// <summary>
        /// Gets the entries of the enumeration.
        /// </summary>
        /// <returns>A dictionation with the enumeration values.</returns>
        public Dictionary<string, EnumEntry> GetEntries();

        /// <summary>
        /// Returns a list of valid enumeration values.
        /// </summary>
        /// <param name="entries">A dictionaty of entries.</param>
        /// <returns>A task.</returns>
        [Obsolete]
        Task GetSymbolics(Dictionary<string, EnumEntry> entries);

        /// <summary>
        /// Gets entry by name.
        /// </summary>
        /// <param name="entryName">The entry name.</param>
        /// <returns>An enumeration entry.</returns>
        [Obsolete]
        public EnumEntry GetEntryByName(string entryName);

        /// <summary>
        /// Gets the entry.
        /// </summary>
        /// <param name="entryValue">The entry index value.</param>
        /// <returns>An enumeration entry.</returns>
        [Obsolete]
        public KeyValuePair<string, EnumEntry> GetEntry(long entryValue);

        /// <summary>
        /// Get the current entry.
        /// </summary>
        /// <param name="entryValue">The entry index value.</param>
        /// <returns>An enumeration entry.</returns>
        [Obsolete]
        public KeyValuePair<string, EnumEntry> GetCurrentEntry();
    }
}