using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GenICam
{
    public class GenEnumeration : GenCategory, IEnumeration
    {
        public GenEnumeration(CategoryProperties categoryProperties, Dictionary<string, EnumEntry> entries, IPValue pValue, Dictionary<string, IMathematical> expressions = null)
        {
            CategoryProperties = categoryProperties;
            Entries = entries;
            PValue = pValue;
            SetValueCommand = new DelegateCommand(ExecuteSetValueCommand);
            GetValueCommand = new DelegateCommand(ExecuteGetValueCommand);
        }
        private async void ExecuteGetValueCommand()
        {
            Value = await GetIntValueAsync();
            ValueToWrite = Value;
            RaisePropertyChanged(nameof(Value));
            RaisePropertyChanged(nameof(ValueToWrite));
        }

        /// <summary>
        /// Enumeration Entry List
        /// </summary>
        public Dictionary<string, EnumEntry> Entries { get; set; }

        /// <summary>
        /// Enumeration Value Parameter
        /// </summary>
        public Int64 Value { get; set; }

        public long ValueToWrite { get; set; }

        public async Task<long> GetIntValueAsync()
        {
            if (PValue is IPValue pValue)
            {
                return (long)await pValue.GetValueAsync();
            }

            return Value;
        }

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
                            pBuffer = BitConverter.GetBytes((UInt16)value);
                            break;

                        case 4:
                            pBuffer = BitConverter.GetBytes((Int32)value);
                            break;

                        case 8:
                            pBuffer = BitConverter.GetBytes(value);
                            break;
                    }

                    var reply = await Register.SetAsync(pBuffer, length);

                    if (reply.IsSentAndReplyReceived && reply.Reply[0] == 0)
                        Value = value;
                }
            }

            ValueToWrite = Entries.Values.ToList().IndexOf(GetCurrentEntry(Value));
            RaisePropertyChanged(nameof(ValueToWrite));
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
            return Entries.Values.FirstOrDefault(x => x.Value == entryValue);
        }

        private async void ExecuteSetValueCommand()
        {
            await SetIntValueAsync(ValueToWrite);
        }

        Task IEnumeration.GetSymbolics(Dictionary<string, EnumEntry> entries)
        {
            throw new NotImplementedException();
        }
    }
}