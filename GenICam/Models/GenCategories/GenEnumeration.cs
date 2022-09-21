using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Prism.Commands;

namespace GenICam
{
    /// <summary>
    /// GenICam Enumeration implementation.
    /// </summary>
    public class GenEnumeration : GenCategory, IEnumeration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenEnumeration"/> class.
        /// </summary>
        /// <param name="categoryProperties">The category properties.</param>
        /// <param name="entries">The entries.</param>
        /// <param name="pValue">The pValue.</param>
        /// <param name="expressions">The expressions.</param>
        public GenEnumeration(CategoryProperties categoryProperties, Dictionary<string, EnumEntry> entries, IPValue pValue, Dictionary<string, IMathematical> expressions = null)
                : base(categoryProperties, pValue)
        {
            Entries = entries;
            SetValueCommand = new DelegateCommand<long>(ExecuteSetValueCommand);
            GetValueCommand = new DelegateCommand(ExecuteGetValueCommand);
        }

        /// <summary>
        /// Gets or sets the enumeration entry list.
        /// </summary>
        public Dictionary<string, EnumEntry> Entries { get; set; }

        /// <summary>
        /// Gets or sets the enumeration value parameter.
        /// </summary>
        public long Value { get; set; }

        /// <inheritdoc/>
        public async Task<long> GetValueAsync()
        {
            if (PValue is IPValue pValue)
            {
                return (long)await pValue.GetValueAsync();
            }

            return Value;
        }

        /// <inheritdoc/>
        public async Task<IReplyPacket> SetValueAsync(long value)
        {
            if (PValue is IPValue pValue)
            {
                if (AccessMode != GenAccessMode.RO)
                {
                    if (Entries.Select(x => x.Value.Value == value).Count() == 0)
                    {
                        throw new GenICamException("Invalid Value been sent", new InvalidDataException());
                    }

                    return await pValue.SetValueAsync(value);
                }

                throw new GenICamException(message: $"Unable to set the register value; it's read only", new AccessViolationException());
            }

            throw new GenICamException(message: $"Unable to set the value, missing register reference", new MissingFieldException());
        }

        /// <inheritdoc/>
        public Dictionary<string, EnumEntry> GetEntries()
        {
            return Entries;
        }

        /// <summary>
        /// Sets the symbolic list of elements to Entries.
        /// </summary>
        /// <param name="list">The list of symobolic elements.</param>
        public void GetSymbolics(Dictionary<string, EnumEntry> list)
        {
            Entries = list;
        }

        /// <inheritdoc/>
        public EnumEntry GetEntryByName(string entryName)
        {
            return Entries[entryName];
        }

        /// <inheritdoc/>
        public EnumEntry GetEntry(long entryValue)
        {
            var entries = Entries.Where(x => x.Value.Value == entryValue);

            if (entries.Any())
            {
                return entries.First().Value;
            }

            return null;
        }

        /// <inheritdoc/>
        public EnumEntry GetCurrentEntry(long entryValue)
        {
            return Entries.Values.FirstOrDefault(x => x.Value == entryValue);
        }

        /// <inheritdoc/>
        Task IEnumeration.GetSymbolics(Dictionary<string, EnumEntry> entries)
        {
            throw new NotImplementedException();
        }

        private async void ExecuteGetValueCommand()
        {
            Value = await GetValueAsync();
            RaisePropertyChanged(nameof(Value));
        }

        private async void ExecuteSetValueCommand(long value)
        {
            await SetValueAsync(value);
        }
    }
}