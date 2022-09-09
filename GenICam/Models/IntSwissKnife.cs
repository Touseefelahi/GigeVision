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
        /// Initializes a new instance of the <see cref="IntSwissKnife"/> class.
        /// </summary>
        /// <param name="formula">The formula to evaluate.</param>
        /// <param name="pVaribles">The variables.</param>
        /// <param name="constants">The contants.</param>
        /// <param name="expressions">The expression.</param>
        public IntSwissKnife(string formula, Dictionary<string, IPValue> pVaribles, Dictionary<string, double> constants = null, Dictionary<string, string> expressions = null)
        {
            PVariables = pVaribles;
            Formula = formula;
            Constants = constants;
            Expressions = expressions;
            // Prepare Expression
            Formula = Formula.Replace(" ", string.Empty);
            // Value = ExecuteFormula();
            Formula = MathParserHelper.PrepareFromula(Formula, Expressions);
        }

        /// <summary>
        /// Calculates the formula and returns the result.
        /// </summary>
        /// <returns>The result as a double.</returns>
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

                    // Keeping the code as may need some implementation.
                    // while (opreations.Any(c => formula.Contains(c)))
                    // {
                    //     formula = EvaluateFormula(formula);
                    //     return Evaluate(formula);
                    // }
                }

            }
            catch (Exception)
            {
                throw;
            }

            return 0;
        }

        /// <summary>
        /// Gets the formula result.
        /// </summary>
        public double Value { get; private set; }

        /// <summary>
        /// Gets or sets SwisKinfe Variable parameters.
        /// </summary>
        private Dictionary<string, IPValue> PVariables { get; set; }

        /// <summary>
        /// Gets or sets the SwisKinfe constants values.
        /// </summary>
        private Dictionary<string, double> Constants { get; set; }

        /// <summary>
        /// Gets or sets the SwisKinfe expressions.
        /// </summary>
        private Dictionary<string, string> Expressions { get; set; }

        /// <summary>
        /// Gets or sets the formula expression.
        /// </summary>
        private string Formula { get; set; }


        /// <summary>
        /// Get SwissKinfe value async.
        /// </summary>
        /// <returns>The result as a long.</returns>
        public async Task<long?> GetValueAsync()
        {
            return (long)await ExecuteFormula();
        }

        /// <summary>
        /// Set SwissKnife value async.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public Task<IReplyPacket> SetValueAsync(long value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Helper To Read SwissKinfe Experssion Parameters.
        /// </summary>
        /// <param name="key">The key to read.</param>
        /// <returns>A task.</returns>
        private async Task ReadExpressionPValues(string key)
        {
            if (key.Equals("BINXFPGA"))
            {
                // To implement.
            }
            double? value = null;
            value = await PVariables[key].GetValueAsync();

            if (value is null)
            {
                throw new Exception("Failed to read register value", new InvalidDataException());
            }

            Formula = Formula.Replace(key, value.ToString());
        }
    }
}