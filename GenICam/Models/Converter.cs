using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace GenICam
{
    /// <summary>
    /// Converter class.
    /// </summary>
    public class Converter : IMathematical, IDoubleValue
    {
        private double value;

        /// <summary>
        /// Initializes a new instance of the <see cref="Converter"/> class.
        /// </summary>
        /// <param name="formulaTo">The formula to convert.</param>
        /// <param name="formulaFrom">The formale from convert.</param>
        /// <param name="pValue">The PValue.</param>
        /// <param name="slope">The splope.</param>
        /// <param name="pVariables">The variables.</param>
        public Converter(string formulaTo, string formulaFrom, IPValue pValue, Slope slope, Dictionary<string, IPValue> pVariables = null)
        {
            // Prepare Expression
            FormulaTo = MathParserHelper.PrepareFromula(formulaTo);
            FormulaFrom = MathParserHelper.PrepareFromula(formulaFrom);

            PVariables = pVariables;
            PValue = pValue;
            Slope = slope;
        }

        /// <summary>
        /// Gets the PValue.
        /// </summary>
        public IPValue PValue { get; private set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public double Value
        {
            get
            {
                return value;
                // Why this commented code?
                //Answer: the following code has been commented  for the implementation test purposes, it could be uncommented and used or removed later, it depends on the final implementation of GenICam Interface.
                // return ExecuteFormulaFrom();
            }

            set
            {
                this.value = value;
                // Why this commented code?
                //Answer: the following code has been commented  for the implementation test purposes, it could be uncommented and used or removed later, it depends on the final implementation of GenICam Interface.
                // Value = ExecuteFormulaTo();
            }
        }

        private Dictionary<string, IPValue> PVariables { get; set; }

        private string FormulaFrom { get; set; }

        private string FormulaTo { get; set; }

        private Slope Slope { get; set; }

        /// <summary>
        /// Gets the value async.
        /// </summary>
        /// <returns>The value as a long.</returns>
        public async Task<long?> GetValueAsync()
        {
            var result = await GetDoubleValueAsync().ConfigureAwait(false);
            return result is null ? null : (long?)Math.Round(result.Value, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Sets the value async.
        /// </summary>
        /// <param name="value">The value as a long.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation..</returns>
        public async Task<IReplyPacket> SetValueAsync(long value)
        {
            return await SetDoubleValueAsync(value).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<double?> GetDoubleValueAsync()
        {
            var result = await ExecuteFormulaFrom().ConfigureAwait(false);
            Value = result;
            return result;
        }

        /// <inheritdoc/>
        public async Task<IReplyPacket> SetDoubleValueAsync(double value)
        {
            var toValue = await ExecuteFormulaTo(value).ConfigureAwait(false);
            Value = value;
            return await PValue.SetValueAsync(toValue).ConfigureAwait(false);
        }

        private async Task<double> ExecuteFormulaFrom()
        {
            try
            {
                var formula = FormulaFrom;
                foreach (var word in formula.Split())
                {
                    if (word.Equals("TO"))
                    {
                        long? value = await PValue.GetValueAsync().ConfigureAwait(false);

                        if (value is null)
                        {
                            throw new GenICamException("Failed to read formula register value", new NullReferenceException());
                        }

                        formula = formula.Replace(word, value.Value.ToString(CultureInfo.InvariantCulture));
                    }

                    if (PVariables != null && PVariables.ContainsKey(word))
                    {
                        long? value = null;

                        value = await PVariables[word].GetValueAsync().ConfigureAwait(false);

                        if (value is null)
                        {
                            throw new GenICamException("Failed to read formula register value", new NullReferenceException());
                        }

                        formula = formula.Replace(word, value.Value.ToString(CultureInfo.InvariantCulture));
                    }
                }

                formula = MathParserHelper.FormatExpression(formula);
                return MathParserHelper.CalculateExpression(formula);
            }
            catch (Exception ex)
            {
                throw new GenICamException(message: $"Failed to calculate the given formula {FormulaFrom}", ex);
            }
        }

        private async Task<long> ExecuteFormulaTo(double value)
        {
            try
            {
                var formula = FormulaTo;
                foreach (var word in formula.Split())
                {
                    if (word.Equals("FROM"))
                    {
                        formula = formula.Replace(word, value.ToString(CultureInfo.InvariantCulture));
                    }

                    if (PVariables != null && PVariables.ContainsKey(word))
                    {
                        long? variableValue = null;
                        variableValue = await PVariables[word].GetValueAsync().ConfigureAwait(false);

                        if (variableValue is null)
                        {
                            throw new GenICamException("Failed to read formula register value", new NullReferenceException());
                        }

                        formula = formula.Replace(word, variableValue.Value.ToString(CultureInfo.InvariantCulture));
                    }
                }

                formula = MathParserHelper.FormatExpression(formula);
                return (long)Math.Round(MathParserHelper.CalculateExpression(formula), MidpointRounding.AwayFromZero);
            }
            catch (Exception ex)
            {
                throw new GenICamException(message: $"Failed to calculate the given formula {FormulaTo}", ex);
            }
        }
    }
}