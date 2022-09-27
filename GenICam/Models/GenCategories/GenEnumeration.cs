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

            SetValueCommand = new DelegateCommand<object>(ExecuteSetValueCommand);
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
        public KeyValuePair<string, EnumEntry> CurrentEnumEntry { get; private set; }

        /// <inheritdoc/>
        public async Task<long> GetValueAsync()
        {
            if (PValue is not null)
            {
                return (long)await PValue.GetValueAsync();
            }

            throw new GenICamException(message: $"Unable to get the value, missing register reference", new MissingFieldException());
        }

        /// <inheritdoc/>
        public async Task<IReplyPacket> SetValueAsync(long value)
        {
            if (PValue is not null)
            {
                try
                {
                    GetEntry(value);
                    return await PValue.SetValueAsync(value);
                }
                catch (Exception ex)
                {
                    throw new GenICamException("Failed to set the value", ex);
                }
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
        public KeyValuePair<string, EnumEntry> GetEntry(long entryValue)
        {
            try
            {
                var entries = Entries.Where(x => x.Value.Value == entryValue);

                if (entries.Any())
                {
                    return entries.First();
                }

                throw new GenICamException("Invalid value", new InvalidDataException());
            }
            catch (ArgumentException ex)
            {
                throw new GenICamException(message: "Failed to get enumerator entry.", ex);
            }
            catch (Exception ex)
            {
                throw new GenICamException(message: "Failed to get enumerator entry.", ex);
            }

        }

        /// <inheritdoc/>
        public KeyValuePair<string, EnumEntry> GetCurrentEntry()
        {
            try
            {
                return CurrentEnumEntry;
            }
            catch (ArgumentException ex)
            {
                throw new GenICamException(message: "Failed to get enumerator current entry.", ex);
            }
            catch (Exception ex)
            {
                throw new GenICamException(message: "Failed to get enumerator current  entry.", ex);
            }
        }

        /// <inheritdoc/>
        Task IEnumeration.GetSymbolics(Dictionary<string, EnumEntry> entries)
        {
            throw new NotImplementedException();
        }

        private async void ExecuteGetValueCommand()
        {
            try
            {
                Value = await GetValueAsync();
                CurrentEnumEntry = GetEntry(Value);
                RaisePropertyChanged(nameof(CurrentEnumEntry));
            }
            catch (Exception ex)
            {
                //ToDo: display exception.
            }
        }

        private async void ExecuteSetValueCommand(object value)
        {
            try
            {
                await SetValueAsync((long)value);
                ExecuteGetValueCommand();
            }
            catch (Exception ex)
            {
                //ToDo: display exception.
            }
        }
    }
}