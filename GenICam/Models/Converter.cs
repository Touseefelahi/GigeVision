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
                // return ExecuteFormulaFrom();
            }

            set
            {
                this.value = value;

                // Why this commented code?
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
            value = (long)await ExecuteFormulaTo();
            return await PValue.SetValueAsync(value);
        }

        private async Task<double> ExecuteFormulaFrom()
        {
            foreach (var word in FormulaFrom.Split())
            {
                if (word.Equals("TO"))
                {
                    long? value = null;

                    value = await ExecuteFormulaTo();

                    if (value is null)
                    {
                        throw new Exception("Failed to read register value", new InvalidDataException());
                    }

                    FormulaFrom = FormulaFrom.Replace(word, string.Format("0x{0:X8}", value));
                }

                if (PVariables.ContainsKey(word))
                {
                    long? value = null;

                    value = await PVariables[word].GetValueAsync();

                    if (value is null)
                    {
                        throw new Exception("Failed to read register value", new InvalidDataException());
                    }

                    FormulaFrom = FormulaFrom.Replace(word, string.Format("0x{0:X8}", value));
                }
            }

            return MathParserHelper.CalculateExpression(FormulaFrom);
        }

        private async Task<long> ExecuteFormulaTo()
        {
            foreach (var word in FormulaTo.Split())
            {
                if (word.Equals("FROM"))
                {
                    long? value = null;

                    value = await PValue.GetValueAsync();

                    if (value is null)
                    {
                        throw new Exception("Failed to read register value", new InvalidDataException());
                    }

                    FormulaTo = FormulaTo.Replace(word, string.Format("0x{0:X8}", value));
                }

                if (PVariables.ContainsKey(word))
                {
                    long? value = null;

                    value = await PVariables[word].GetValueAsync();

                    if (value is null)
                    {
                        throw new Exception("Failed to read register value", new InvalidDataException());
                    }

                    FormulaTo = FormulaTo.Replace(word, string.Format("0x{0:X8}", value));
                }
            }

            return (long)MathParserHelper.CalculateExpression(FormulaTo);
        }
    }
}