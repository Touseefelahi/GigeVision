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
        /// <param name="pMin">The address pointer to min value.</param>
        /// <param name="pMax">The address pointer to max value.</param>
        /// <param name="inc">The increment.</param>
        /// <param name="incMode">The increment mode.</param>
        /// <param name="representation">The representation.</param>
        /// <param name="value">The value.</param>
        /// <param name="unit">The unit.</param>
        /// <param name="pValue">The PValue.</param>
        /// <param name="expressions">The expressions.</param>
        public GenFloat(CategoryProperties categoryProperties, double min, double max, IPValue pMin, IPValue pMax, long inc, IncrementMode incMode, Representation representation, double value, string unit, IPValue pValue)
                : base(categoryProperties, pValue)
        {
            PMax = pMax;
            PMin = pMin;
            Min = min;
            Max = max;
            Inc = inc;
            IncMode = incMode;
            Representation = representation;
            Value = value;
            Unit = unit;
            SetValueCommand = new DelegateCommand<object>(ExecuteSetValueCommand);
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
        public IncrementMode? IncMode { get; private set; }

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

            if (IncMode != null)
            {
                throw new GenICamException(message: $"Unable to get the increment value, Increment mode is {Enum.GetName((IncrementMode)IncMode)}", new InvalidOperationException());
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

            if (IncMode != null)
            {
                throw new GenICamException(message: $"Unable to get the valid values list, Increment mode is {Enum.GetName((IncrementMode)IncMode)}", new InvalidOperationException());
            }

            throw new GenICamException(message: $"Unable to get the increment value, Increment mode is missing", new NullReferenceException());
        }

        /// <inheritdoc/>
        /// <exception cref="NotImplementedException">Not yet implemented.</exception>
        public async Task<double> GetMaxAsync()
        {
            if (PMax is IDoubleValue doubleMax)
            {
                var result = await doubleMax.GetDoubleValueAsync().ConfigureAwait(false);
                return result ?? Max;
            }

            if (PMax is not null)
            {
                var result = await PMax.GetValueAsync().ConfigureAwait(false);
                return result ?? Max;
            }

            return Max;
        }

        /// <inheritdoc/>
        /// <exception cref="NotImplementedException">Not yet implemented.</exception>
        public async Task<double> GetMinAsync()
        {
            if (PMin is IDoubleValue doubleMin)
            {
                var result = await doubleMin.GetDoubleValueAsync().ConfigureAwait(false);
                return result ?? Min;
            }

            if (PMin is not null)
            {
                var result = await PMin.GetValueAsync().ConfigureAwait(false);
                return result ?? Min;
            }

            return Min;
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
            return Unit;
        }

        /// <inheritdoc/>
        public async Task<long?> GetValueAsync()
        {
            if (PValue is IDoubleValue doubleValue)
            {
                var result = await doubleValue.GetDoubleValueAsync().ConfigureAwait(false);
                if (result is not null)
                {
                    Value = result.Value;
                    return (long)Math.Round(result.Value, MidpointRounding.AwayFromZero);
                }
            }

            if (PValue is not null)
            {
                var result = await PValue.GetValueAsync().ConfigureAwait(false);
                if (result is not null)
                {
                    Value = result.Value;
                    return result.Value;
                }
            }

            throw new GenICamException(message: $"Unable to set the value, missing register reference", new MissingFieldException());
        }

        /// <inheritdoc/>
        public async Task<IReplyPacket> SetValueAsync(long value)
        {
            if (PValue is IDoubleValue doubleValue)
            {
                Value = value;
                return await doubleValue.SetDoubleValueAsync(value).ConfigureAwait(false);
            }

            if (PValue is IPValue pValue)
            {
                Value = value;
                return await pValue.SetValueAsync(value).ConfigureAwait(false);
            }

            throw new GenICamException(message: $"Unable to set the value, missing register reference", new MissingFieldException());
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
        Task<double?> IFloat.GetValueAsync()
        {
            if (PValue is IDoubleValue doubleValue)
            {
                return doubleValue.GetDoubleValueAsync();
            }

            return GetValueAsDoubleAsync();
        }

        /// <inheritdoc/>
        public async Task SetValueAsync(double value)
        {
            if (PValue is IDoubleValue doubleValue)
            {
                await doubleValue.SetDoubleValueAsync(value).ConfigureAwait(false);
                Value = value;
                return;
            }

            if (PValue is IPValue pValue)
            {
                await pValue.SetValueAsync((long)Math.Round(value, MidpointRounding.AwayFromZero)).ConfigureAwait(false);
                Value = value;
                return;
            }

            throw new GenICamException(message: $"Unable to set the value, missing register reference", new MissingFieldException());
        }

        /// <inheritdoc/>
        long IFloat.GetDisplayPrecision()
        {
            return DisplayPrecision;
        }

        /// <inheritdoc/>
        public Task ImposeMinAsync(long min)
        {
            Min = min;
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task ImposeMaxAsync(long max)
        {
            Max = max;
            return Task.CompletedTask;
        }

        private async void ExecuteSetValueCommand(object value)
        {
            try
            {
                await SetValueAsync((double)value);
                ExecuteGetValueCommand();
            }
            catch (Exception ex)
            {
                //ToDo: display exception.
            }
        }

        private async void ExecuteGetValueCommand()
        {
            try
            {
                Value = await GetValueAsDoubleAsync().ConfigureAwait(false) ?? Value;
                RaisePropertyChanged(nameof(Value));
            }
            catch (Exception ex)
            {
                //ToDo: display exception.
            }
        }

        private async Task<double?> GetValueAsDoubleAsync()
        {
            if (PValue is IDoubleValue doubleValue)
            {
                return await doubleValue.GetDoubleValueAsync().ConfigureAwait(false);
            }

            if (PValue is not null)
            {
                var result = await PValue.GetValueAsync().ConfigureAwait(false);
                return result;
            }

            throw new GenICamException(message: $"Unable to get the value, missing register reference", new MissingFieldException());
        }
    }
}