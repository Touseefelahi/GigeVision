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
                : base(categoryProperties, pValue)
        {
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

            PMax = pMax;
            PMin = pMin;

            // Control Commands
            GetValueCommand = new DelegateCommand(ExecuteGetValueCommand);
            SetValueCommand = new DelegateCommand<long>(ExecuteSetValueCommand);
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
                    Value = (long)(await PValue.GetValueAsync());
                    return Value;
                }

                throw new GenICamException(message: $"Unable to get the register value; it's write only", new AccessViolationException());
            }

            throw new GenICamException(message: $"Unable to get the value, missing register reference", new MissingFieldException());
        }

        /// <inheritdoc/>
        public async Task<IReplyPacket> SetValueAsync(long value)
        {
            if (PValue is IPValue pValue)
            {
                return await pValue.SetValueAsync(value);
            }

            throw new GenICamException(message: $"Unable to set the value, missing register reference", new MissingFieldException());
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

                throw new GenICamException(message: $"Unable to Get the register value; it's write only", new AccessViolationException());
            }

            throw new GenICamException(message: $"Unable to get the value, missing register reference", new MissingFieldException());
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

                throw new GenICamException(message: $"Unable to Get the register value; it's write only", new AccessViolationException());
            }

            throw new GenICamException(message: $"Unable to get the value, missing register reference", new MissingFieldException());
        }

        /// <inheritdoc/>
        public long? GetIncrement()
        {
            if (IncMode == IncrementMode.fixedIncrement)
            {
                return Inc;
            }

            if (IncMode != null)
            {
                throw new GenICamException(message: $"Unable to get the increment value, Increment mode is {Enum.GetName((IncrementMode)IncMode)}", new InvalidOperationException());
            }

            throw new GenICamException(message: $"Unable to get the increment value, Increment mode is missing", new NullReferenceException());
        }

        /// <inheritdoc/>
        public List<long> GetListOfValidValue()
        {
            if (IncMode == IncrementMode.listIncrement)
            {
                return ListOfValidValue;
            }

            if (IncMode != null)
            {
                throw new GenICamException(message: $"Unable to get the valid values list, Increment mode is {Enum.GetName((IncrementMode)IncMode)}", new InvalidOperationException());
            }

            throw new GenICamException(message: $"Unable to get the increment value, Increment mode is missing", new NullReferenceException());
        }

        /// <inheritdoc/>
        public IncrementMode GetIncrementMode()
        {
            if (IncMode is null)
            {
                throw new GenICamException(message: $"Unable to get the increment mode value, Increment mode is missing", new NullReferenceException());
            }

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
            RaisePropertyChanged(nameof(Value));
        }

        private async void ExecuteSetValueCommand(long value)
        {
            if (Value != value)
            {
                await SetValueAsync(value);
            }
        }

    }
}