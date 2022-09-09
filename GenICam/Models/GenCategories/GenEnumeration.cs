using System;
using System.Collections.Generic;
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
        {
            CategoryProperties = categoryProperties;
            Entries = entries;
            PValue = pValue;
            SetValueCommand = new DelegateCommand(ExecuteSetValueCommand);
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

        /// <summary>
        /// Gets or sets the value to write.
        /// </summary>
        public long ValueToWrite { get; set; }

        /// <inheritdoc/>
        public async Task<long> GetIntValueAsync()
        {
            if (PValue is IPValue pValue)
            {
                return (long)await pValue.GetValueAsync();
            }

            return Value;
        }

        /// <inheritdoc/>
        public async Task SetIntValueAsync(long value)
        {
            if (PValue is IRegister Register)
            {
                if (Register.AccessMode != GenAccessMode.RO)
                {
                    if (Entries.Select(x => x.Value.Value == value).Count() == 0)
                    {
                        return;
                    }

                    var length = Register.GetLength();
                    byte[] pBuffer = new byte[length];

                    switch (length)
                    {
                        case 2:
                            pBuffer = BitConverter.GetBytes((ushort)value);
                            break;

                        case 4:
                            pBuffer = BitConverter.GetBytes((int)value);
                            break;

                        case 8:
                            pBuffer = BitConverter.GetBytes(value);
                            break;
                    }

                    var reply = await Register.SetAsync(pBuffer, length);

                    if (reply.IsSentAndReplyReceived && reply.Reply[0] == 0)
                    {
                        Value = value;
                    }
                }
            }

            ValueToWrite = Entries.Values.ToList().IndexOf(GetCurrentEntry(Value));
            RaisePropertyChanged(nameof(ValueToWrite));
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
            Value = await GetIntValueAsync();
            ValueToWrite = Value;
            RaisePropertyChanged(nameof(Value));
            RaisePropertyChanged(nameof(ValueToWrite));
        }

        private async void ExecuteSetValueCommand()
        {
            await SetIntValueAsync(ValueToWrite);
        }
    }
}