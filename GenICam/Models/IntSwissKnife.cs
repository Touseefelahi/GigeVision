using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace GenICam
{
    /// <summary>
    /// this is a mathematical class for register parameter computations.
    /// </summary>
    public class IntSwissKnife : IMathematical, IDoubleValue
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
            var result = await GetDoubleValueAsync().ConfigureAwait(false);
            return result is null ? null : (long?)Math.Round(result.Value, MidpointRounding.AwayFromZero);
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

        /// <inheritdoc/>
        public async Task<double?> GetDoubleValueAsync()
        {
            var result = await ExecuteFormula().ConfigureAwait(false);
            Value = result;
            return result;
        }

        /// <inheritdoc/>
        public Task<IReplyPacket> SetDoubleValueAsync(double value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Calculates the formula and returns the result.
        /// </summary>
        /// <returns>The result as a double.</returns>
        private async Task<double> ExecuteFormula()
        {
            try
            {
                var formula = Formula;
                var resolvedExpressions = Expressions is null ? null : new Dictionary<string, string>(Expressions);

                foreach (var pVariable in PVariables)
                {
                    var value = await pVariable.Value.GetValueAsync().ConfigureAwait(false);
                    if (value is null)
                    {
                        throw new GenICamException("Failed to read expression register", new NullReferenceException());
                    }

                    var formattedValue = value.Value.ToString(CultureInfo.InvariantCulture);

                    if (resolvedExpressions?.Count > 0)
                    {
                        foreach (var expressionKey in resolvedExpressions.Keys.ToList())
                        {
                            resolvedExpressions[expressionKey] = resolvedExpressions[expressionKey].Replace(pVariable.Key, formattedValue);
                        }
                    }

                    formula = formula.Replace(pVariable.Key, formattedValue);
                }

                if (Constants?.Count > 0)
                {
                    foreach (var constant in Constants)
                    {
                        var formattedConstant = constant.Value.ToString(CultureInfo.InvariantCulture);
                        if (resolvedExpressions?.Count > 0)
                        {
                            foreach (var expressionKey in resolvedExpressions.Keys.ToList())
                            {
                                resolvedExpressions[expressionKey] = resolvedExpressions[expressionKey].Replace(constant.Key, formattedConstant);
                            }
                        }

                        formula = formula.Replace(constant.Key, formattedConstant);
                    }
                }

                if (resolvedExpressions?.Count > 0)
                {
                    foreach (var expression in resolvedExpressions)
                    {
                        formula = formula.Replace(expression.Key, $"({MathParserHelper.CalculateExpression(expression.Value)})");
                    }
                }

                formula = MathParserHelper.FormatExpression(formula);
                return MathParserHelper.CalculateExpression(formula);
            }
            catch (Exception ex)
            {
                throw new GenICamException(message: $"Failed to calculate the given formula {Formula}", ex);
            }
        }

        /// <summary>
        /// Helper To Read SwissKinfe Expression Parameters.
        /// </summary>
        /// <param name="key">The key to read.</param>
        /// <returns>A task.</returns>
        private async Task ReadExpressionPValues(string key)
        {
            long? value = await PVariables[key].GetValueAsync();

            if (value is null)
            {
                throw new GenICamException("Failed to read expression register", new NullReferenceException());
            }

            Formula = Formula.Replace(key, value.ToString());
        }
    }
}