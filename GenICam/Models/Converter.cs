using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GenICam
{
    public class Converter : IMathematical
    {
        public Converter(string formulaTo, string formulaFrom, IPValue pValue, Slope slope, Dictionary<string, IPValue> pVariables = null)
        {
            FormulaTo = formulaTo;
            FormulaFrom = formulaFrom;

            //Prepare Expression
            FormulaTo = FormulaTo.Replace(" ", "");
            FormulaFrom = FormulaFrom.Replace(" ", "");
            List<char> opreations = new List<char> { '(', '+', '-', '/', '*', '=', '?', ':', ')', '>', '<', '&', '|', '^', '~', '%' };

            foreach (var character in opreations)
            {
                if (opreations.Where(x => x == character).Count() > 0)
                {
                    FormulaTo = FormulaTo.Replace($"{character}", $" {character} ");
                    FormulaFrom = FormulaFrom.Replace($"{character}", $" {character} ");
                }
            }

            PVariables = pVariables;
            PValue = pValue;
            Slope = slope;

            ExecuteFormulaFrom().ConfigureAwait(false);
            ExecuteFormulaTo().ConfigureAwait(false);
        }

        public IPValue PValue { get; private set; }

        public Task<double> Value
        {
            get
            {
                return ExecuteFormulaFrom();
            }
            set
            {
                Value = ExecuteFormulaTo();
            }
        }

        private Dictionary<string, IPValue> PVariables { get; set; }
        private string FormulaFrom { get; set; }
        private string FormulaTo { get; set; }

        private Slope Slope { get; set; }

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

        public async Task<long> GetValue()
        {
            return await PValue.GetValue().ConfigureAwait(false);
        }

        public async Task<IReplyPacket> SetValue(long value)
        {
            return await PValue.SetValue(value).ConfigureAwait(false);
        }

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

        private async Task<double> ExecuteFormulaFrom()
        {
            foreach (var word in FormulaFrom.Split())
            {
                if (word.Equals("TO"))
                {
                    double? value = null;

                    value = await PValue.GetValue().ConfigureAwait(false);

                    if (value is null)
                        throw new Exception("Failed to read register value", new InvalidDataException());

                    FormulaFrom = FormulaFrom.Replace(word, value.ToString());
                }

                foreach (var pVariable in PVariables)
                {
                    if (pVariable.Key.Equals(word))
                    {
                        double? value = null;

                        value = await pVariable.Value.GetValue().ConfigureAwait(false);

                        if (value is null)
                            throw new Exception("Failed to read register value", new InvalidDataException());

                        FormulaFrom = FormulaFrom.Replace(word, value.ToString());
                    }
                }
            }

            double result;
            if (FormulaFrom != string.Empty)
            {
                string formula = FormulaFrom;
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

        private async Task<double> ExecuteFormulaTo()
        {
            foreach (var word in FormulaTo.Split())
            {
                if (word.Equals("FROM"))
                {
                    double? value = null;

                    value = await PValue.GetValue().ConfigureAwait(false);

                    if (value is null)
                        throw new Exception("Failed to read register value", new InvalidDataException());

                    FormulaTo = FormulaTo.Replace(word, value.ToString());
                }

                foreach (var pVariable in PVariables)
                {
                    if (pVariable.Key.Equals(word))
                    {
                        double? value = null;

                        value = await pVariable.Value.GetValue().ConfigureAwait(false);

                        if (value is null)
                            throw new Exception("Failed to read register value", new InvalidDataException());

                        FormulaTo = FormulaTo.Replace(word, value.ToString());
                    }
                }
            }

            double result;
            if (FormulaTo != string.Empty)
            {
                string formula = FormulaTo;
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
    }
}