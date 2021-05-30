using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GenICam
{
    public class GenEnumeration : GenCategory, IGenEnumeration
    {
        public GenEnumeration(CategoryProperties categoryProperties, Dictionary<string, EnumEntry> entries, IPValue pValue, Dictionary<string, IMathematical> expressions = null)
        {
            CategoryProperties = categoryProperties;
            Entries = entries;
            PValue = pValue;
            Expressions = expressions;
            SetValueCommand = new DelegateCommand(ExecuteSetValueCommand);
            if (CategoryProperties.Visibility != GenVisibility.Invisible)
                SetupFeatures();
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

        public async Task<long> GetIntValue()
        {
            if (PValue is IRegister register)
            {
                if (register.AccessMode != GenAccessMode.WO)
                    return await PValue.GetValue().ConfigureAwait(false);
            }
            else if (PValue is IntSwissKnife intSwissKnife)
            {
                return await intSwissKnife.GetValue().ConfigureAwait(false);
            }

            return Value;
        }

        public async void SetIntValue(long value)
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

                    var reply = await Register.Set(pBuffer, length).ConfigureAwait(false);

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

        public async void SetupFeatures()
        {
            Value = (long)(await GetIntValue());
            ValueToWrite = Entries.Values.ToList().IndexOf(GetCurrentEntry(Value));
        }

        private void ExecuteSetValueCommand()
        {
            SetIntValue(ValueToWrite);
        }
    }
}