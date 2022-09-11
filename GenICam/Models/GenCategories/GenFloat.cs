using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Prism.Commands;

namespace GenICam
{
    /// <summary>
    /// GenICam float implementation.
    /// </summary>
    public class GenFloat : GenCategory, IFloat
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenFloat"/> class.
        /// </summary>
        /// <param name="categoryProperties">the category properties.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <param name="inc">The increment.</param>
        /// <param name="incMode">The increment mode.</param>
        /// <param name="representation">The representation.</param>
        /// <param name="value">The value.</param>
        /// <param name="unit">The unit.</param>
        /// <param name="pValue">The PValue.</param>
        /// <param name="expressions">The expressions.</param>
        public GenFloat(CategoryProperties categoryProperties, double min, double max, long inc, IncrementMode incMode, Representation representation, double value, string unit, IPValue pValue, Dictionary<string, IMathematical> expressions)
        {
            CategoryProperties = categoryProperties;
            Min = min;
            Max = max;
            Inc = inc;
            IncMode = incMode;
            Representation = representation;
            Value = value;
            Unit = unit;
            PValue = pValue;
            SetValueCommand = new DelegateCommand(ExecuteSetValueCommand);
            GetValueCommand = new DelegateCommand(ExecuteGetValueCommand);
        }

        /// <summary>
        /// Gets the minimum value.
        /// </summary>
        public double Min { get; private set; }

        /// <summary>
        /// Gets the maximum value.
        /// </summary>
        public double Max { get; private set; }

        /// <summary>
        /// Gets the increment.
        /// </summary>
        public long Inc { get; private set; } = 1;

        /// <summary>
        /// Gets the increment mode.
        /// </summary>
        public IncrementMode IncMode { get; private set; }

        /// <summary>
        /// Gets the representation.
        /// </summary>
        public Representation Representation { get; private set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Gets the list of valid values.
        /// </summary>
        public List<double> ListOfValidValue { get; private set; }

        /// <summary>
        /// Gets the unit.
        /// </summary>
        public string Unit { get; private set; }

        /// <summary>
        /// Gets the display notation.
        /// </summary>
        public DisplayNotation DisplayNotation { get; private set; }

        /// <summary>
        /// Gets the display precision.
        /// </summary>
        public uint DisplayPrecision { get; private set; }

        /// <summary>
        /// Gets or sets the value to write.
        /// </summary>
        public double ValueToWrite { get; set; }

        /// <summary>
        /// Gets the display alias.
        /// </summary>
        /// <returns>The display alias.</returns>
        /// <exception cref="NotImplementedException">Not yet implemented.</exception>
        public IFloat GetFloatAlias()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public DisplayNotation GetDisplayNotation()
        {
            return DisplayNotation;
        }

        /// <summary>
        /// Gets the display precision.
        /// </summary>
        /// <returns>The display precision.</returns>
        public uint GetDisplayPrecision()
        {
            return DisplayPrecision;
        }

        /// <inheritdoc/>
        /// <exception cref="NotImplementedException">Not yet implemented.</exception>
        public IEnumeration GetEnumAlias()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public long? GetIncrement()
        {
            if (IncMode == IncrementMode.fixedIncrement)
            {
                return Inc;
            }
            else
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public IncrementMode GetIncrementMode()
        {
            return IncMode;
        }

        /// <inheritdoc/>
        public IInteger GetIntAlias()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public List<double> GetListOfValidValue()
        {
            if (IncMode == IncrementMode.listIncrement)
            {
                return ListOfValidValue;
            }
            else
            {
                return null;
            }
        }

        /// <inheritdoc/>
        /// <exception cref="NotImplementedException">Not yet implemented.</exception>
        public async Task<double> GetMaxAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        /// <exception cref="NotImplementedException">Not yet implemented.</exception>
        public async Task<double> GetMinAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Representation GetRepresentation()
        {
            return Representation;
        }

        /// <inheritdoc/>
        /// <exception cref="NotImplementedException">Not yet implemented.</exception>
        public string GetUnit()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<long?> GetValueAsync()
        {
            if (PValue is IRegister register)
            {
                // TODO: check this dead code.
                ////if (register.AccessMode != GenAccessMode.WO)
                ////{
                ////    byte[] pBuffer = BitConverter.GetBytes(await register.GetValueAsync());

                ////    if (Representation == Representation.HexNumber)
                ////        Array.Reverse(pBuffer);

                ////    switch (pBuffer.Length)
                ////    {
                ////        case 2:
                ////            return BitConverter.ToUInt16(pBuffer); ;

                ////        case 4:
                ////            return BitConverter.ToUInt32(pBuffer);

                ////        case 8:
                ////            return BitConverter.ToInt64(pBuffer);

                ////    }
                ////}
            }
            else if (PValue is IPValue pValue)
            {
                return await pValue.GetValueAsync();
            }

            return (long)Value;
        }

        /// <inheritdoc/>
        public async Task<IReplyPacket> SetValueAsync(long value)
        {
            IReplyPacket reply = null;

            if (PValue is IRegister register)
            {
                if (register.AccessMode != GenAccessMode.RO)
                {
                    if ((value % Inc) != 0)
                    {
                        return null;
                    }

                    var length = register.GetLength();
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

                    reply = await register.SetAsync(pBuffer, length);
                    if (reply.IsSentAndReplyReceived && reply.Reply[0] == 0)
                    {
                        Value = value;
                    }
                }
            }

            ValueToWrite = Value;
            RaisePropertyChanged(nameof(ValueToWrite));
            return reply;
        }

        /// <summary>
        /// Imposes a maximum.
        /// </summary>
        /// <param name="max">The maximum.</param>
        public void ImposeMax(double max)
        {
            Max = max;
        }

        /// <summary>
        /// Imposes a minimum.
        /// </summary>
        /// <param name="min">The minimum.</param>
        public void ImposeMin(double min)
        {
            Min = min;
        }

        /// <inheritdoc/>
        /// <exception cref="NotImplementedException">Not yet implemented.</exception>
        Task<double> IFloat.GetValueAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        /// <exception cref="NotImplementedException">Not yet implemented.</exception>
        public Task SetValueAsync(double value)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        /// <exception cref="NotImplementedException">Not yet implemented.</exception>
        long IFloat.GetDisplayPrecision()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        /// <exception cref="NotImplementedException">Not yet implemented.</exception>
        public Task ImposeMinAsync(long min)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        /// <exception cref="NotImplementedException">Not yet implemented.</exception>
        public Task ImposeMaxAsync(long max)
        {
            throw new NotImplementedException();
        }

        private async void ExecuteSetValueCommand()
        {
            if (Value != ValueToWrite)
            {
                await SetValueAsync(ValueToWrite);
            }
        }

        private async void ExecuteGetValueCommand()
        {
            Value = (long)await GetValueAsync();
            ValueToWrite = Value;
            RaisePropertyChanged(nameof(Value));
            RaisePropertyChanged(nameof(ValueToWrite));
        }
    }
}