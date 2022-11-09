using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GenICam
{
    /// <summary>
    /// Converter class.
    /// </summary>
    public class Converter : IMathematical
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
            return (long)(await ExecuteFormulaFrom());
        }

        /// <summary>
        /// Sets the value async.
        /// </summary>
        /// <param name="value">The value as a long.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation..</returns>
        public async Task<IReplyPacket> SetValueAsync(long value)
        {
            return await SetValueAsync();
        }

        private async Task<IReplyPacket> SetValueAsync()
        {
            var value = await ExecuteFormulaTo();
            return await PValue.SetValueAsync(value);
        }

        private async Task<double> ExecuteFormulaFrom()
        {
            var formulaFrom = FormulaFrom;
            try
            {
                foreach (var word in formulaFrom.Split())
                {
                    if (word.Equals("TO"))
                    {
                        long? value = null;

                        value = await PValue.GetValueAsync();

                        if (value is null)
                        {
                            throw new GenICamException("Failed to read formula register value", new NullReferenceException());
                        }

                        formulaFrom = formulaFrom.Replace(word, string.Format("h.{0:X8}", value));
                    }

                    if (PVariables.ContainsKey(word))
                    {
                        long? value = null;

                        value = await PVariables[word].GetValueAsync();

                        if (value is null)
                        {
                            throw new GenICamException("Failed to read formula register value", new NullReferenceException());
                        }

                        formulaFrom = formulaFrom.Replace(word, string.Format(format: "h.{0:X8}", value));
                    }
                }
                formulaFrom = MathParserHelper.FormatExpression(formulaFrom);
                return MathParserHelper.CalculateExpression(formulaFrom);
            }
            catch (Exception ex)
            {
                throw new GenICamException(message: $"Failed to calculate the given formula {FormulaFrom}", ex);
            }
        }

        private async Task<long> ExecuteFormulaTo()
        {
            try
            {
                var formulaTo = FormulaTo;

                foreach (var word in formulaTo.Split())
                {
                    if (word.Equals("FROM"))
                    {
                        long? value = await PValue.GetValueAsync();

                        if (value is null)
                        {
                            throw new GenICamException("Failed to read formula register value", new NullReferenceException());
                        }

                        formulaTo = formulaTo.Replace(word, string.Format("h.{0:X8}", value));
                    }

                    if (PVariables.ContainsKey(word))
                    {
                        long? value = null;

                        value = await PVariables[word].GetValueAsync();

                        if (value is null)
                        {
                            throw new GenICamException("Failed to read formula register value", new NullReferenceException());
                        }

                        formulaTo = formulaTo.Replace(word, string.Format("h.{0:X8}", value));
                    }
                }
                formulaTo = MathParserHelper.FormatExpression(formulaTo);
                return (long)MathParserHelper.CalculateExpression(formulaTo);
            }
            catch (Exception ex)
            {
                throw new GenICamException(message: $"Failed to calculate the given formula {FormulaTo}", ex);
            }
        }
    }
}