using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using org.mariuszgromada.math.mxparser;

namespace GenICam
{
    /// <summary>
    /// Math parser helper.
    /// </summary>
    public class MathParserHelper
    {
        private static readonly List<char> opreations = new() { '(', '+', '-', '/', '*', '=', '?', ':', ')', '>', '<', '&', '|', '^', '~', '%' };

        /// <summary>
        /// Prepare a formula.
        /// </summary>
        /// <param name="formula">The formula.</param>
        /// <param name="expressions">The dictionnary for the expressions.</param>
        /// <returns>A new formula with changes from the expressions.</returns>
        public static string PrepareFromula(string formula, Dictionary<string, string> expressions = null)
        {
            foreach (var character in opreations)
            {
                if (opreations.Where(x => x == character).Any())
                {
                    formula = formula.Replace($"{character}", $" {character} ");
                    if (expressions != null)
                    {
                        foreach (var expression in expressions.ToList())
                        {
                            expressions[expression.Key] = expression.Value.Replace($"{character}", $" {character} ");
                        }
                    }
                }
            }

            return formula;
        }

        /// <summary>
        /// Calculate an expression.
        /// </summary>
        /// <param name="experssion">The expression to calculate.</param>
        /// <returns>The result.</returns>
        public static double CalculateExpression(string experssion)
        {
            try
            {
                if (experssion.Contains("?"))
                {
                    var cases = experssion.Split(':', 2);
                    var subs = cases[0].Split("?", 2);

                    experssion = $"if({GetBracketed(subs[0])}, {GetBracketed(subs[1])}, {CalculateExpression(GetBracketed(cases[1]))})";
                }

                return new Expression(experssion).calculate();
            }
            catch (Exception ex)
            {

                throw new GenICamException(message: "XMath parser has failed to calculate formula", ex);
            }
        }

        /// <summary>
        /// Gets the bracketed formula.
        /// </summary>
        /// <param name="formula">The formlua to process.</param>
        /// <returns>The formula in brackets.</returns>
        public static string GetBracketed(string formula)
        {
            int openBracketCounter = 0;
            int charIndex = 0;
            foreach (var character in formula)
            {
                if (character == '(')
                {
                    openBracketCounter++;
                }

                if (character == ')' && openBracketCounter < 1)
                {
                    formula = formula.Remove(charIndex, 1);
                    charIndex--;
                }
                else if (character == ')')
                {
                    openBracketCounter--;
                }

                charIndex++;
            }

            while (openBracketCounter > 0)
            {
                formula = formula.Remove(0, 1);
                --openBracketCounter;
            }

            return formula;
        }

        /// <summary>
        /// Format the expression.
        /// </summary>
        /// <param name="formula">The formula ro process.</param>
        /// <returns>A formated expression.</returns>
        public static string FormatExpression(string formula)
        {
            formula = formula.Replace(" ", string.Empty);

            return formula.Replace("0x", "h.")
                .Replace("|", "@|")
                .Replace("&", "@&")
                .Replace("~", "@~")
                .Replace("^", "@^")
                .Replace("<<", "@<<")
                .Replace(">>", "@>>");
        }

        /// <summary>
        /// Evaluates the formula expression.
        /// </summary>
        /// <param name="expression">The expression to evaluate.</param>
        /// <returns>The calculation of the pression.</returns>
        [Obsolete]
        public static double Evaluate(string expression)
        {
            expression = "( " + expression + " )";
            foreach (var character in opreations)
            {
                if (opreations.Where(x => x == character).Any())
                {
                    expression = expression.Replace($"{character}", $" {character} ");
                }
            }

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
                    values.Push(long.Parse(word[2..], System.Globalization.NumberStyles.HexNumber));

                    // valuesList.Last().Push(values.Peek());
                }
                else if (double.TryParse(word, out double tempNumber))
                {
                    if (tempBoolean)
                    {
                        return tempNumber;
                    }

                    values.Push(tempNumber);

                    // valuesList.Last().Push(values.Peek());
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
                            // valuesList.Add(new Stack<double>());
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
                                string opreator = string.Empty;

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
                                            {
                                                return values.Pop();
                                            }
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
            {
                return values.Pop();
            }

            if (tempBoolean)
            {
                return 1;
            }
            else
            {
                return 0;
            }

            throw new InvalidDataException("Failed to read the formula");
        }

        [Obsolete]
        private static bool DoMathOpreation(string opreator, Stack<string> opreators, Stack<double> values)
        {
            bool tempBoolean = false;
            double value = 0;
            int integerValue = 0;

            // ToDo: Implement (&&) , (||) Operators
            if (values.Count > 1)
            {
                if (opreator.Equals("+"))
                {
                    value = (double)values.Pop();
                    if (opreators.Count > 0 && values.Count > 0)
                    {
                        if (opreators.Peek().Equals("*"))
                        {
                            tempBoolean = DoMathOpreation(opreators.Pop(), opreators, values);
                        }

                        if (opreators.Peek().Equals("/"))
                        {
                            tempBoolean = DoMathOpreation(opreators.Pop(), opreators, values);
                        }
                    }

                    value = (double)values.Pop() + value;
                    values.Push(value);
                }
                else if (opreator.Equals("-"))
                {
                    value = (double)values.Pop();
                    if (opreators.Count > 0 && values.Count > 0)
                    {
                        if (opreators.Peek().Equals("*"))
                        {
                            tempBoolean = DoMathOpreation(opreators.Pop(), opreators, values);
                        }

                        if (opreators.Peek().Equals("/"))
                        {
                            tempBoolean = DoMathOpreation(opreators.Pop(), opreators, values);
                        }
                    }

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
                    {
                        tempBoolean = true;
                    }
                }
                else if (opreator.Equals(">="))
                {
                    integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                    if (GetLongValueFromString(values.Pop().ToString()) >= integerValue)
                    {
                        tempBoolean = true;
                    }
                }
                else if (opreator.Equals("<="))
                {
                    integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                    if (GetLongValueFromString(values.Pop().ToString()) <= integerValue)
                    {
                        tempBoolean = true;
                    }
                }
                else if (opreator.Equals("&"))
                {
                    if (values.Count > 1)
                    {
                        var byte2 = (int)GetLongValueFromString(values.Pop().ToString());
                        var byte1 = (int)GetLongValueFromString(values.Pop().ToString());
                        integerValue = byte1 & byte2;
                        values.Push(integerValue);
                    }
                }
                else if (opreator.Equals("|"))
                {
                    if (values.Count > 1)
                    {
                        var byte2 = (int)GetLongValueFromString(values.Pop().ToString());
                        var byte1 = (int)GetLongValueFromString(values.Pop().ToString());
                        integerValue = byte1 | byte2;
                        values.Push(integerValue);
                    }
                }
                else if (opreator.Equals("^"))
                {
                    if (values.Count > 2)
                    {
                        var byte2 = (int)GetLongValueFromString(values.Pop().ToString());
                        var byte1 = (int)GetLongValueFromString(values.Pop().ToString());
                        integerValue = byte1 ^ byte2;
                        values.Push(integerValue);
                    }
                }
                else if (opreator.Equals(">"))
                {
                    integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                    if (GetLongValueFromString(values.Pop().ToString()) > integerValue)
                    {
                        tempBoolean = true;
                    }
                }
                else if (opreator.Equals(">>"))
                {
                    integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                    integerValue = (int)GetLongValueFromString(values.Pop().ToString()) >> integerValue;
                    values.Push(integerValue);
                }
                else if (opreator.Equals("<"))
                {
                    integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                    if (GetLongValueFromString(values.Pop().ToString()) < integerValue)
                    {
                        tempBoolean = true;
                    }
                }
                else if (opreator.Equals("<<"))
                {
                    integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                    integerValue = (int)GetLongValueFromString(values.Pop().ToString()) << integerValue;
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
        /// Parse Hexdecimal String to Actual Integer.
        /// </summary>
        /// <param name="value">The string value to parse.</param>
        /// <returns>The hexadecimal value as a number.</returns>
        [Obsolete]
        private static long GetLongValueFromString(string value)
        {
            if (value.StartsWith("0x"))
            {
                value = value.Replace("0x", string.Empty);
                return long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            }

            try
            {
                return long.Parse(value);
            }
            catch (Exception)
            {
                // Just catching all for simplicity.
            }

            return 0;
        }

        [Obsolete]
        private string EvaluateFormula(string formula)
        {
            double result;
            string equation;

            foreach (var item in formula.Split('(', StringSplitOptions.None))
            {
                equation = item;
                if (item.Contains(')'))
                {
                    equation = item.Substring(0, item.IndexOf(')'));
                }

                if (equation.Contains('+') || equation.Contains('-') || equation.Contains('/') || equation.Contains('*'))
                {
                    var last = equation.Replace(" ", string.Empty);
                    formula = formula.Replace(" ", string.Empty);

                    // last = last.Substring(0, last.Length - 1);
                    if (last != "+" || last != "-" || last != "/" || last != "*")
                    {
                        result = Evaluate(last);
                        if (formula.Contains($"({last})"))
                        {
                            formula = formula.Replace($"({last})", result.ToString());
                        }
                        else
                        {
                            formula = formula.Replace(last, result.ToString());
                        }
                    }
                }

                if (formula.Contains($"({equation})"))
                {
                    formula = formula.Replace($"({equation})", equation);
                }
            }

            if (formula.Contains("("))
            {
                formula = EvaluateFormula(formula);
            }

            return formula;
        }
    }
}
