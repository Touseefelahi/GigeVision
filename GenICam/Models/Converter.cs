using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GenICam
{
    public class Converter : IMathematical
    {
        public Converter(string formulaTo, string formulaFrom, IPValue pValue, Slope slope, Dictionary<string, IPValue> pVariables = null)
        {
            //Prepare Expression
            FormulaTo = MathParserHelper.PrepareFromula(formulaTo);
            FormulaFrom = MathParserHelper.PrepareFromula(formulaFrom);

            PVariables = pVariables;
            PValue = pValue;
            Slope = slope;
        }

        public IPValue PValue { get; private set; }

        private double value;

        public double Value
        {
            get
            {
                return value;
                //return ExecuteFormulaFrom();
            }
            set
            {
                this.value = value;
                //Value = ExecuteFormulaTo();
            }
        }

        private Dictionary<string, IPValue> PVariables { get; set; }
        private string FormulaFrom { get; set; }
        private string FormulaTo { get; set; }

        private Slope Slope { get; set; }

        public async Task<long?> GetValueAsync()
        {
            return (long)(await ExecuteFormulaFrom());
        }

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
                        throw new Exception("Failed to read register value", new InvalidDataException());

                    FormulaFrom = FormulaFrom.Replace(word, string.Format("0x{0:X8}", value));
                }

                if (PVariables.ContainsKey(word))
                {
                    long? value = null;

                    value = await PVariables[word].GetValueAsync();

                    if (value is null)
                        throw new Exception("Failed to read register value", new InvalidDataException());

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
                        throw new Exception("Failed to read register value", new InvalidDataException());

                    FormulaTo = FormulaTo.Replace(word, string.Format("0x{0:X8}", value));
                }

                if (PVariables.ContainsKey(word))
                {
                    long? value = null;

                    value = await PVariables[word].GetValueAsync();

                    if (value is null)
                        throw new Exception("Failed to read register value", new InvalidDataException());

                    FormulaTo = FormulaTo.Replace(word, string.Format("0x{0:X8}", value));
                }
            }
            return (long)MathParserHelper.CalculateExpression(FormulaTo);
        }
    }
}