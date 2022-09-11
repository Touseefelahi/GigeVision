using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Prism.Commands;

namespace GenICam
{
    /// <summary>
    /// Merges the integer’s value, min, max, and increment properties from different nodes.
    /// </summary>
    public class GenInteger : GenCategory, IInteger
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenInteger"/> class.
        /// </summary>
        /// <param name="categoryProperties">The category properties.</param>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        /// <param name="inc">The increment.</param>
        /// <param name="pMax">The pointer on a maximum mathematical value.</param>
        /// <param name="pMin">The pointer on a minimum mathamatical value.</param>
        /// <param name="pInc">The pointer on an increment mathematical value.</param>
        /// <param name="incMode">The increment mode.</param>
        /// <param name="representation">The representation.</param>
        /// <param name="value">The value.</param>
        /// <param name="unit">The unit.</param>
        /// <param name="pValue">The PValue.</param>
        public GenInteger(CategoryProperties categoryProperties, long? min, long? max, long? inc, IMathematical pMax, IMathematical pMin, IMathematical pInc, IncrementMode? incMode, Representation representation, long? value, string unit, IPValue pValue)
        {
            CategoryProperties = categoryProperties;

            if (min is not null)
            {
                Min = (long)min;
            }

            if (max is not null)
            {
                Max = (long)max;
            }

            if (inc is not null)
            {
                Inc = (long)inc;
            }

            if (value is not null)
            {
                Value = (long)value;
            }

            IncMode = incMode;
            Representation = representation;

            Unit = unit;

            PValue = pValue;
            PMax = pMax;
            PMin = pMin;

            // Control Commands
            GetValueCommand = new DelegateCommand(ExecuteGetValueCommand);
            SetValueCommand = new DelegateCommand(ExecuteSetValueCommand);
        }

        /// <summary>
        /// Gets the represenation.
        /// </summary>
        public Representation Representation { get; private set; }

        /// <summary>
        /// Gets the valid value set.
        /// </summary>
        public string ValidValuesSet { get; private set; }

        /// <summary>
        /// Gets the minimum value.
        /// </summary>
        public long Min { get; private set; }

        /// <summary>
        /// Gets the maximum value.
        /// </summary>
        public long Max { get; private set; }

        /// <summary>
        /// Gets the increment.
        /// </summary>
        public long Inc { get; private set; } = 1;

        /// <summary>
        /// Gets or sets a value indicating whether if this is streamable.
        /// </summary>
        public bool Streamble { get; set; }

        /// <summary>
        /// Gets the increment mode.
        /// </summary>
        public IncrementMode? IncMode { get; private set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public long? Value { get; set; }

        /// <summary>
        /// Gets the list of valid values.
        /// </summary>
        public List<long> ListOfValidValue { get; private set; }

        /// <summary>
        /// Gets the unit.
        /// </summary>
        public string Unit { get; private set; } = string.Empty;

        /// <summary>
        /// Gets or sets the value to write.
        /// </summary>
        public long ValueToWrite { get; set; }

        /// <summary>
        /// Gets the pointer on the mathematical maximum value.
        /// </summary>
        public IMathematical PMax { get; }

        /// <summary>
        /// Gets the pointer on the mathematical minimum value.
        /// </summary>
        public IMathematical PMin { get; }

        /// <inheritdoc/>
        public async Task<long?> GetValueAsync()
        {
            if (PValue is IRegister register)
            {
                if (register.AccessMode != GenAccessMode.WO)
                {
                    return (long)(await PValue.GetValueAsync());
                }
            }

            return Value;
        }

        /// <inheritdoc/>
        public async Task<IReplyPacket> SetValueAsync(long value)
        {
            IReplyPacket reply = null;
            if (PValue is IRegister Register)
            {
                if (Register.AccessMode != GenAccessMode.RO)
                {
                    if ((value % Inc) == 0 && (value <= await GetMaxAsync() && value >= await GetMinAsync()))
                    {
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

                        reply = await Register.SetAsync(pBuffer, length);
                        if (reply.IsSentAndReplyReceived && reply.Reply[0] == 0)
                        {
                            Value = value;
                        }
                    }
                }
            }

            ValueToWrite = (long)Value;
            RaisePropertyChanged(nameof(ValueToWrite));
            return reply;
        }

        /// <summary>
        /// Gets the minimum value async.
        /// </summary>
        /// <returns>The minimum value or 0 if not set.</returns>
        public async Task<long> GetMinAsync()
        {
            if (PMin is IRegister register)
            {
                if (register.AccessMode != GenAccessMode.WO)
                {
                    return (long)(await PValue.GetValueAsync());
                }
            }

            return 0;
        }

        /// <summary>
        /// Gets the maximum value async.
        /// </summary>
        /// <returns>The maximum value or 0 if not set.</returns>
        public async Task<long> GetMaxAsync()
        {
            if (PMax is IRegister register)
            {
                if (register.AccessMode != GenAccessMode.WO)
                {
                    return (long)(await PValue.GetValueAsync());
                }
            }

            return 0;
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
        public List<long> GetListOfValidValue()
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
        public IncrementMode GetIncrementMode()
        {
            return (IncrementMode)IncMode;
        }

        /// <inheritdoc/>
        public Representation GetRepresentation()
        {
            return Representation;
        }

        /// <inheritdoc/>
        public string GetUnit()
        {
            return Unit;
        }

        /// <summary>
        /// Imposes the miminum value.
        /// </summary>
        /// <param name="min">The minimum value to impose.</param>
        /// <exception cref="NotImplementedException">Not implemented yet.</exception>
        public void ImposeMin(long min)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Imposes the maximum value.
        /// </summary>
        /// <param name="min">The maximum value to impose.</param>
        /// <exception cref="NotImplementedException">Not implemented yet.</exception>
        public void ImposeMax(long max)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        /// <exception cref="NotImplementedException">Not implemented yet.</exception>
        public IFloat GetFloatAlias()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        /// <exception cref="NotImplementedException">Not implemented yet.</exception>
        Task<long> IInteger.GetMaxAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        /// <exception cref="NotImplementedException">Not implemented yet.</exception>
        Task<long> IInteger.GetMinAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        /// <exception cref="NotImplementedException">Not implemented yet.</exception>
        public Task<IReplyPacket> ImposeMinAsync(long value)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        /// <exception cref="NotImplementedException">Not implemented yet.</exception>
        public Task<IReplyPacket> ImposeMaxAsync(long value)
        {
            throw new NotImplementedException();
        }

        private async void ExecuteGetValueCommand()
        {
            Value = await GetValueAsync();
            ValueToWrite = (long)Value;
            RaisePropertyChanged(nameof(Value));
            RaisePropertyChanged(nameof(ValueToWrite));
        }

        private async void ExecuteSetValueCommand()
        {
            if (Value != ValueToWrite)
            {
                await SetValueAsync(ValueToWrite);
            }
        }

    }
}