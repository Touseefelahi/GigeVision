using System;
using System.Collections;
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
            List<char> opreations = new List<char> { '(', '+', '-', '/', '*', '=', '?', ':', ')', '>', '<', '&', '|', '^', '~', '%' };

            foreach (var character in opreations)
            {
                if (opreations.Where(x => x == character).Count() > 0)
                {
                    Formula = Formula.Replace($"{character}", $" {character} ");
                    if (Expressions != null)
                    {
                        foreach (var expression in Expressions.ToList())
                        {
                            Expressions[expression.Key] = expression.Value.Replace($"{character}", $" {character} ");
                        }
                    }
                }
            }

            Value = ExecuteFormula();
        }

        /// <summary>
        /// Formula Result
        /// </summary>
        public Task<double> Value { get; private set; }

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
        /// this method evaluate the formula expression
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static double Evaluate(string expression)
        {
            expression = "( " + expression + " )";
            List<char> opreations = new List<char> { '(', '+', '-', '/', '*', '=', '?', ':', ')', '>', '<', '&', '|', '^', '~', '%' };
            foreach (var character in opreations)
                if (opreations.Where(x => x == character).Count() > 0)
                    expression = expression.Replace($"{character}", $" {character} ");

            Stack<string> opreators = new Stack<string>();
            Stack<double> values = new Stack<double>();
            bool tempBoolean = false;
            bool isPower = false;
            bool isLeft = false;
            bool isRight = false;

            foreach (var word in expression.Split())
            {
                if (word.StartsWith("0x"))
                {
                    values.Push(Int64.Parse(word.Substring(2), System.Globalization.NumberStyles.HexNumber));
                    //valuesList.Last().Push(values.Peek());
                }
                else if (double.TryParse(word, out double tempNumber))
                {
                    if (tempBoolean)
                        return tempNumber;

                    values.Push(tempNumber);
                    //valuesList.Last().Push(values.Peek());
                }
                else
                {
                    switch (word)
                    {
                        case "*":
                            if (isPower)
                            {
                                opreators.Pop();
                                opreators.Push("**");
                                isPower = false;
                            }
                            else
                            {
                                isPower = true;
                                opreators.Push(word);
                            }
                            isLeft = false;
                            isRight = false;
                            break;

                        case ">":
                            if (isRight)
                            {
                                opreators.Pop();
                                opreators.Push(">>");
                                isRight = false;
                            }
                            else if (isLeft)
                            {
                                opreators.Pop();
                                opreators.Push("<>");
                            }
                            else
                            {
                                opreators.Push(word);
                                isRight = true;
                            }

                            isPower = false;
                            isLeft = false;
                            break;

                        case "<":
                            if (isLeft)
                            {
                                opreators.Pop();
                                opreators.Push("<<");
                                isLeft = false;
                            }
                            else
                            {
                                opreators.Push(word);
                                isLeft = true;
                            }
                            isPower = false;
                            isRight = false;
                            break;

                        case "=":
                            if (isLeft)
                            {
                                opreators.Pop();
                                opreators.Push("<=");
                            }
                            else if (isRight)
                            {
                                opreators.Pop();
                                opreators.Push(">=");
                            }
                            else
                            {
                                opreators.Push(word);
                            }
                            isPower = false;
                            isRight = false;
                            isLeft = false;
                            break;

                        case "(":
                            //valuesList.Add(new Stack<double>());
                            opreators.Push(word);
                            isPower = false;
                            isLeft = false;
                            isRight = false;
                            break;

                        case "+":
                        case "-":
                        case "/":
                        case "?":
                        case ":":
                        case "&":
                        case "|":
                        case "%":
                        case "^":
                        case "~":
                        case "ATAN":
                        case "COS":
                        case "SIN":
                        case "TAN":
                        case "ABS":
                        case "EXP":
                        case "LN":
                        case "LG":
                        case "SQRT":
                        case "TRUNC":
                        case "FLOOR":
                        case "CELL":
                        case "ROUND":
                        case "ASIN":
                        case "ACOS":
                        case "SGN":
                        case "NEG":
                        case "E":
                        case "PI":
                            opreators.Push(word);
                            isPower = false;
                            isLeft = false;
                            isRight = false;
                            break;

                        case ")":
                            while (values.Count > 0 && opreators.Count > 0)
                            {
                                string opreator = "";

                                opreator = opreators.Pop();
                                tempBoolean = DoMathOpreation(opreator, opreators, values);

                                if (opreators.Count > 0)
                                {
                                    if (opreators.Peek().Equals("?"))
                                    {
                                        opreators.Pop();
                                        if (tempBoolean)
                                        {
                                            if (values.Count > 0)
                                                return values.Pop();
                                        }
                                    }
                                    else if (opreators.Peek().Equals("("))
                                    {
                                        opreators.Pop();
                                    }
                                }
                            }

                            isPower = false;
                            isLeft = false;
                            isRight = false;
                            break;

                        case "":

                            break;

                        default:
                            isPower = false;
                            isLeft = false;
                            isRight = false;
                            break;
                    }
                }
            }

            if (values.Count > 0)
                return values.Pop();

            if (tempBoolean)
                return 1;
            else
                return 0;
            throw new InvalidDataException("Failed to read the formula");
        }

        /// <summary>
        /// Get SwissKinfe Value
        /// </summary>
        /// <returns></returns>
        public async Task<Int64> GetValue()
        {
            return (Int64)await ExecuteFormula().ConfigureAwait(false);
        }

        /// <summary>
        /// Set SwissKnife Value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public Task<IReplyPacket> SetValue(long value)
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
        private static bool DoMathOpreation(string opreator, Stack<string> opreators, Stack<double> values)
        {
            bool tempBoolean = false;
            double value = 0;
            int integerValue = 0;
            //ToDo: Implement (&&) , (||) Operators
            if (values.Count > 1)
            {
                if (opreator.Equals("+"))
                {
                    if (opreators.Count > 0 && values.Count > 0)
                    {
                        if (opreators.Peek().Equals("*"))
                            tempBoolean = DoMathOpreation(opreators.Pop(), opreators, values);

                        if (opreators.Peek().Equals("/"))
                            tempBoolean = DoMathOpreation(opreators.Pop(), opreators, values);
                    }

                    value = (double)values.Pop();
                    value = (double)values.Pop() + value;
                    values.Push(value);
                }
                else if (opreator.Equals("-"))
                {
                    if (opreators.Count > 0 && values.Count > 0)
                    {
                        if (opreators.Peek().Equals("*"))
                            tempBoolean = DoMathOpreation(opreators.Pop(), opreators, values);

                        if (opreators.Peek().Equals("/"))
                            tempBoolean = DoMathOpreation(opreators.Pop(), opreators, values);
                    }
                    value = (double)values.Pop();
                    value = (double)values.Pop() - value;
                    values.Push(value);
                }
                else if (opreator.Equals("*"))
                {
                    value = (double)values.Pop();
                    value = (double)values.Pop() * value;
                    values.Push(value);
                }
                else if (opreator.Equals("**"))
                {
                    value = (double)values.Pop();
                    value = Math.Pow(values.Pop(), value);
                    values.Push(value);
                }
                else if (opreator.Equals("/"))
                {
                    value = (double)values.Pop();
                    value = (double)values.Pop() / value;
                    values.Push(value);
                }
                else if (opreator.Equals("="))
                {
                    var firstValue = (int)GetLongValueFromString(values.Pop().ToString());
                    var secondValue = (int)GetLongValueFromString(values.Pop().ToString());

                    if (secondValue == firstValue)
                    {
                        tempBoolean = true;
                    }
                }
                else if (opreator.Equals("<>"))
                {
                    var firstValue = (int)GetLongValueFromString(values.Pop().ToString());
                    var secondValue = (int)GetLongValueFromString(values.Pop().ToString());

                    if (secondValue != firstValue)
                        tempBoolean = true;
                }
                else if (opreator.Equals(">="))
                {
                    integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                    if (GetLongValueFromString(values.Pop().ToString()) >= integerValue)
                        tempBoolean = true;
                }
                else if (opreator.Equals("<="))
                {
                    integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                    if (GetLongValueFromString(values.Pop().ToString()) <= integerValue)
                        tempBoolean = true;
                }
                else if (opreator.Equals("&"))
                {
                    if (values.Count > 1)
                    {
                        var byte2 = (int)GetLongValueFromString(values.Pop().ToString());
                        var byte1 = (int)GetLongValueFromString(values.Pop().ToString());
                        integerValue = (byte1 & byte2);
                        values.Push(integerValue);
                    }
                }
                else if (opreator.Equals("|"))
                {
                    if (values.Count > 1)
                    {
                        var byte2 = (int)GetLongValueFromString(values.Pop().ToString());
                        var byte1 = (int)GetLongValueFromString(values.Pop().ToString());
                        integerValue = (byte1 | byte2);
                        values.Push(integerValue);
                    }
                }
                else if (opreator.Equals("^"))
                {
                    if (values.Count > 2)
                    {
                        var byte2 = (int)GetLongValueFromString(values.Pop().ToString());
                        var byte1 = (int)GetLongValueFromString(values.Pop().ToString());
                        integerValue = (byte1 ^ byte2);
                        values.Push(integerValue);
                    }
                }
                else if (opreator.Equals(">"))
                {
                    integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                    if (GetLongValueFromString(values.Pop().ToString()) > integerValue)
                        tempBoolean = true;
                }
                else if (opreator.Equals(">>"))
                {
                    integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                    integerValue = ((int)GetLongValueFromString(values.Pop().ToString()) >> integerValue);
                    values.Push(integerValue);
                }
                else if (opreator.Equals("<"))
                {
                    integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                    if (GetLongValueFromString(values.Pop().ToString()) < integerValue)
                        tempBoolean = true;
                }
                else if (opreator.Equals("<<"))
                {
                    integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                    integerValue = ((int)GetLongValueFromString(values.Pop().ToString()) << integerValue);
                    values.Push(integerValue);
                }
            }
            if (opreator.Equals(":"))
            {
            }
            else if (opreator.Equals("~"))
            {
                integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                integerValue = ~integerValue;
                values.Push(integerValue);
            }
            else if (opreator.Equals("ATAN"))
            {
                values.Push(Math.Atan(values.Pop()));
            }
            else if (opreator.Equals("COS"))
            {
                values.Push(Math.Cos(values.Pop()));
            }
            else if (opreator.Equals("SIN"))
            {
                values.Push(Math.Sin(values.Pop()));
            }
            else if (opreator.Equals("TAN"))
            {
                values.Push(Math.Tan(values.Pop()));
            }
            else if (opreator.Equals("ABS"))
            {
                values.Push(Math.Abs(values.Pop()));
            }
            else if (opreator.Equals("EXP"))
            {
                values.Push(Math.Exp(values.Pop()));
            }
            else if (opreator.Equals("LN"))
            {
                values.Push(Math.Log(values.Pop()));
            }
            else if (opreator.Equals("LG"))
            {
                values.Push(Math.Log10(values.Pop()));
            }
            else if (opreator.Equals("SQRT"))
            {
                values.Push(Math.Sqrt(values.Pop()));
            }
            else if (opreator.Equals("TRUNC"))
            {
                values.Push(Math.Truncate(values.Pop()));
            }
            else if (opreator.Equals("FLOOR"))
            {
                values.Push(Math.Floor(values.Pop()));
            }
            else if (opreator.Equals("CELL"))
            {
                values.Push(Math.Ceiling(values.Pop()));
            }
            else if (opreator.Equals("ROUND"))
            {
                values.Push(Math.Round(values.Pop()));
            }
            else if (opreator.Equals("ASIN"))
            {
                values.Push(Math.Asin(values.Pop()));
            }
            else if (opreator.Equals("ACOS"))
            {
                values.Push(Math.Acos(values.Pop()));
            }
            else if (opreator.Equals("TAN"))
            {
                values.Push(Math.Tan(values.Pop()));
            }

            return tempBoolean;
        }

        /// <summary>
        /// Parse Hexdecimal String to Actual Integer
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static long GetLongValueFromString(string value)
        {
            if (value.StartsWith("0x"))
            {
                value = value.Replace("0x", "");
                return long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            }

            try
            {
                return long.Parse(value); ;
            }
            catch (Exception ex)
            {
            }
            return 0;
        }

        /// <summary>
        /// this method calculates the formula and returns the result
        /// </summary>
        /// <param name="intSwissKnife"></param>
        /// <returns></returns>
        private async Task<double> ExecuteFormula()
        {
            if (Expressions != null)
            {
                foreach (var expression in Expressions.ToList())
                {
                    foreach (var word in expression.Value.Split())
                    {
                        await ReadExpressionPValues(word).ConfigureAwait(false);

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

            foreach (var word in Formula.Split())
            {
                await ReadExpressionPValues(word).ConfigureAwait(false);

                if (Constants != null)
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

                if (Expressions != null)
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

            double result;
            if (Formula != string.Empty)
            {
                string formula = Formula;
                string equation = "";
                while (formula.Contains('+') || formula.Contains('-') || formula.Contains('/') || formula.Contains('*'))
                {
                    foreach (var item in formula.Split('(', StringSplitOptions.None))
                    {
                        equation = item;
                        if (item.Contains(')'))
                            equation = item.Substring(0, item.IndexOf(')'));

                        if (equation.Contains('+') || equation.Contains('-') || equation.Contains('/') || equation.Contains('*'))
                        {
                            var last = equation.Replace(" ", "");
                            last = last.Substring(0, last.Length - 1);
                            if (last != "+" || last != "-" || last != "/" || last != "*")
                            {
                                result = Evaluate(last);
                                if (formula.Contains($"({last})"))
                                    formula = formula.Replace($"({last})", result.ToString());
                                else
                                    formula = formula.Replace(last, result.ToString());
                            }
                        }

                        if (formula.Contains($"({equation})"))
                            formula = formula.Replace($"({equation})", equation);
                    }
                    return Evaluate(formula);
                }
            }

            return 0;
        }

        /// <summary>
        /// Helper To Read SwissKinfe Experssion Parameters
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        private async Task ReadExpressionPValues(string word)
        {
            if (PVariables != null)
            {
                foreach (var pVariable in PVariables)
                {
                    if (pVariable.Key.Equals(word))
                    {
                        double? value = null;
                        value = await pVariable.Value.GetValue().ConfigureAwait(false);

                        if (value is null)
                            throw new Exception("Failed to read register value", new InvalidDataException());
                        Formula = Formula.Replace(word, value.ToString());
                        break;
                    }
                }
            }
        }
    }
}