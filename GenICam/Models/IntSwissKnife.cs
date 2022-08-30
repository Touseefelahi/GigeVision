using org.mariuszgromada.math.mxparser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GenICam
{
    /// <summary>
    /// this is a mathematical class for register parameter computations
    /// </summary>
    public class IntSwissKnife : IMathematical
    {

        /// <summary>
        /// Main Method that calculate the given formula
        /// </summary>
        /// <param name="gvcp"></param>
        /// <param name="formula"></param>
        /// <param name="pVarible"></param>
        /// <param name="value"></param>
        public IntSwissKnife(string formula, Dictionary<string, IPValue> pVaribles, Dictionary<string, double> constants = null, Dictionary<string, string> expressions = null)
        {
            PVariables = pVaribles;
            Formula = formula;
            Constants = constants;
            Expressions = expressions;
            //Prepare Expression
            Formula = Formula.Replace(" ", "");
            //Value = ExecuteFormula();
            Formula = MathParserHelper.PrepareFromula(Formula, Expressions);
        }
        /// <summary>
        /// this method calculates the formula and returns the result
        /// </summary>
        /// <param name="intSwissKnife"></param>
        /// <returns></returns>
        private async Task<double> ExecuteFormula()
        {
            try
            {
                if (Expressions != null)
                {
                    foreach (var expression in Expressions.ToList())
                    {
                        foreach (var word in expression.Value.Split())
                        {
                            if (PVariables.ContainsKey(word))
                            {
                                await ReadExpressionPValues(word);
                            }

                            if (Constants.ContainsKey(word))
                            {
                                foreach (var constant in Constants)
                                {
                                    if (constant.Key.Equals(word))
                                    {
                                        Expressions[expression.Key] = expression.Value.Replace(word, constant.Value.ToString());
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (var word in Formula.Split())
                {
                    if (PVariables.ContainsKey(word))
                    {

                        await ReadExpressionPValues(word);
                    }

                    if (Constants != null)
                    {
                        if (Constants.ContainsKey(word))
                        {
                            foreach (var constant in Constants)
                            {
                                if (constant.Key.Equals(word))
                                {
                                    Formula = Formula.Replace(word, constant.Value.ToString());

                                    break;
                                }
                            }
                        }
                    }

                    if (Expressions != null)
                    {

                        if (Expressions.ContainsKey(word))
                        {
                            foreach (var expression in Expressions)
                            {
                                if (expression.Key.Equals(word))
                                {
                                    Formula = Formula.Replace(word, expression.Value);
                                    break;
                                }
                            }
                        }
                    }
                }

                if (Formula != string.Empty)
                {
                    string formula = Formula;

                    return (double)MathParserHelper.CalculateExpression(formula);

                    //while (opreations.Any(c => formula.Contains(c)))
                    //{
                    //    formula = EvaluateFormula(formula);
                    //    return Evaluate(formula);
                    //}
                }

            }
            catch (Exception ex)
            {

                throw;
            }

            return 0;
        }

        /// <summary>
        /// Formula Result
        /// </summary>
        public double Value { get; private set; }

        /// <summary>
        /// SwisKinfe Variable Parameters
        /// </summary>
        private Dictionary<string, IPValue> PVariables { get; set; }

        /// <summary>
        /// SwisKinfe Constants Values
        /// </summary>
        private Dictionary<string, double> Constants { get; set; }

        /// <summary>
        /// SwisKinfe Expressions
        /// </summary>
        private Dictionary<string, string> Expressions { get; set; }

        /// <summary>
        /// Formula Expression
        /// </summary>
        private string Formula { get; set; }


        /// <summary>
        /// Get SwissKinfe Value
        /// </summary>
        /// <returns></returns>
        public async Task<long?> GetValueAsync()
        {
            return (Int64)await ExecuteFormula();
        }

        /// <summary>
        /// Set SwissKnife Value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public Task<IReplyPacket> SetValueAsync(long value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Helper To Calculate Math Opreations
        /// </summary>
        /// <param name="opreator"></param>
        /// <param name="opreators"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        /// <summary>
        /// Helper To Read SwissKinfe Experssion Parameters
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        private async Task ReadExpressionPValues(string key)
        {
            if (key.Equals("BINXFPGA"))
            {

            }
            double? value = null;
            value = await PVariables[key].GetValueAsync();

            if (value is null)
                throw new Exception("Failed to read register value", new InvalidDataException());
            Formula = Formula.Replace(key, value.ToString());
        }
    }
}