using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GenICam
{
    public class GenEnumeration : GenCategory, IGenEnumeration
    {
        /// <summary>
        /// Enumeration Entry List
        /// </summary>
        public Dictionary<string, EnumEntry> Entries { get; set; }

        /// <summary>
        /// Enumeration Value Parameter
        /// </summary>
        public Int64 Value { get; set; }

        public long ValueToWrite { get; set; }

        public GenEnumeration(CategoryProperties categoryProperties, Dictionary<string, EnumEntry> entries, IPValue pValue, Dictionary<string, IntSwissKnife> expressions = null)
        {
            CategoryProperties = categoryProperties;
            Entries = entries;
            PValue = pValue;
            Expressions = expressions;
            SetValueCommand = new DelegateCommand(ExecuteSetValueCommand);
            if (CategoryProperties.Visibility != GenVisibility.Invisible)
                SetupFeatures();
        }

        public async Task<long> GetIntValue()
        {
            if (PValue is IRegister Register)
            {
                if (Register.AccessMode != GenAccessMode.WO)
                {
                    var length = Register.GetLength();
                    byte[] pBuffer;

                    var reply = await Register.Get(length);
                    if (reply.IsSentAndReplyReceived && reply.Reply[0] == 0)
                    {
                        if (reply.MemoryValue != null)
                            pBuffer = reply.MemoryValue;
                        else
                            pBuffer = BitConverter.GetBytes(reply.RegisterValue);

                        switch (length)
                        {
                            case 2:
                                return BitConverter.ToUInt16(pBuffer);

                            case 4:
                                return BitConverter.ToUInt32(pBuffer);

                            case 8:
                                return BitConverter.ToInt64(pBuffer);
                        }
                    }
                };
            }
            else if (PValue is IntSwissKnife intSwissKnife)
            {
                return (Int64)intSwissKnife.Value;
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

                    var reply = await Register.Set(pBuffer, length);

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
            return Entries.Values.Where(x => x.Value == entryValue).FirstOrDefault();
        }

        public async void SetupFeatures()
        {
            Value = await GetIntValue();

            ValueToWrite = Entries.Values.ToList().IndexOf(GetCurrentEntry(Value));
        }

        private void ExecuteSetValueCommand()
        {
            SetIntValue(ValueToWrite);
        }
    }
}