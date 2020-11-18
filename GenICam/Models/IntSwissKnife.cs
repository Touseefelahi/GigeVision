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
    public class IntSwissKnife : IPValue
    {
        /// <summary>
        /// Math Variable Parameter
        /// </summary>
        public Dictionary<string, object> PVariables { get; set; }

        /// <summary>
        /// Formula Expression
        /// </summary>
        public string Formula { get; private set; }

        /// <summary>
        /// Formula Result
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Main Method that calculate the given formula
        /// </summary>
        /// <param name="gvcp"></param>
        /// <param name="formula"></param>
        /// <param name="pVarible"></param>
        /// <param name="value"></param>
        public IntSwissKnife(string formula, Dictionary<string, object> pVaribles)
        {
            PVariables = pVaribles;
            Formula = formula;

            //Prepare Expression
            Formula = Formula.Replace(" ", "");
            List<char> opreations = new List<char> { '(', '+', '-', '/', '*', '=', '?', ':', ')', '>', '<', '&', '|', '^', '~', '%' };

            foreach (var character in opreations)
                if (opreations.Where(x => x == character).Count() > 0)
                    Formula = Formula.Replace($"{character}", $" {character} ");

            ExecuteFormula(this).ConfigureAwait(false);
        }

        /// <summary>
        /// this method calculates the formula and returns the result
        /// </summary>
        /// <param name="intSwissKnife"></param>
        /// <returns></returns>
        public async Task<double> ExecuteFormula(IntSwissKnife intSwissKnife)
        {
            foreach (var word in intSwissKnife.Formula.Split())
            {
                foreach (var pVariable in PVariables)
                {
                    if (pVariable.Key.Equals(word))
                    {
                        string value = "";
                        //ToDo : Cover all cases
                        if (pVariable.Value is GenInteger integer)
                            value = (await integer.GetValue()).ToString();
                        else if (pVariable.Value is GenIntReg intReg)
                            value = (await intReg.GetValue()).ToString();
                        else if (pVariable.Value is GenMaskedIntReg genMaskedIntReg)
                            value = (await genMaskedIntReg.GetValue()).ToString();
                        else if (pVariable.Value is IntSwissKnife intSwissKnife1)
                            value = (await intSwissKnife1.GetValue()).ToString();
                        else if (pVariable.Value is GenFloat genFloat)
                            value = (await genFloat.GetValue()).ToString();

                        if (value == "")
                            throw new Exception("Failed to read register value", new InvalidDataException());

                        intSwissKnife.Formula = intSwissKnife.Formula.Replace(word, value);
                        break;
                    }
                }
            }

            try
            {
                if (Formula != string.Empty)
                {
                    var value = Evaluate(intSwissKnife.Formula);
                    Value = value;
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
            }
            return Value;
        }

        /// <summary>
        /// this method evaluate the formula expression
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        private double Evaluate(string expression)
        {
            expression = "( " + expression + " )";
            Stack<string> opreators = new Stack<string>();
            Stack<double> values = new Stack<double>();
            bool tempBoolean = false;
            int integerValue = 0;

            foreach (var word in expression.Split())
            {
                if (word.StartsWith("0x"))
                    values.Push(Int64.Parse(word.Substring(2), System.Globalization.NumberStyles.HexNumber));
                else if (Int64.TryParse(word, out Int64 tempNumber))
                    values.Push(tempNumber);
                else
                {
                    if (word.Equals("(")) { continue; }
                    else if (word.Equals("+")) opreators.Push(word);
                    else if (word.Equals("-")) opreators.Push(word);
                    else if (word.Equals("*")) opreators.Push(word);
                    else if (word.Equals("/")) opreators.Push(word);
                    else if (word.Equals("=")) opreators.Push(word);
                    else if (word.Equals("?")) opreators.Push(word);
                    else if (word.Equals(":")) opreators.Push(word);
                    else if (word.Equals("&")) opreators.Push(word);
                    else if (word.Equals("|")) opreators.Push(word);
                    else if (word.Equals(">")) opreators.Push(word);
                    else if (word.Equals("<")) opreators.Push(word);
                    else if (word.Equals("%")) opreators.Push(word);
                    else if (word.Equals("^")) opreators.Push(word);
                    else if (word.Equals("~")) opreators.Push(word);
                    else if (word.Equals(")"))
                    {
                        while (opreators.Count > 0)
                        {
                            string opreator = opreators.Pop();
                            double value = 0;
                            //ToDo: Implement (&&) , (||) Operators
                            if (opreator.Equals("+"))
                            {
                                value = (double)values.Pop();
                                value = (double)values.Pop() + value;
                                values.Push(value);
                            }
                            else if (opreator.Equals("-"))
                            {
                                value = (double)values.Pop();
                                value = (double)values.Pop() - value;
                                values.Push(value);
                            }
                            else if (opreator.Equals("*"))
                            {
                                if (opreators.Count > 0)
                                {
                                    switch (opreators.Peek())
                                    {
                                        case "*":
                                            value = (double)values.Pop();
                                            value = value * value;
                                            values.Push(value);
                                            break;

                                        default:
                                            value = (double)values.Pop();
                                            value = (double)values.Pop() * value;
                                            values.Push(value);
                                            break;
                                    }
                                }
                                else
                                {
                                    value = (double)values.Pop();
                                    value = (double)values.Pop() * value;
                                    values.Push(value);
                                }
                            }
                            else if (opreator.Equals("/"))
                            {
                                value = (double)values.Pop();
                                value = (double)values.Pop() / value;
                                values.Push(value);
                            }
                            else if (opreator.Equals("="))
                            {
                                if (opreators.Count > 0)
                                {
                                    switch (opreators.Peek())
                                    {
                                        case ">":
                                            opreators.Pop();
                                            integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                                            if (GetLongValueFromString(values.Pop().ToString()) >= integerValue)
                                                tempBoolean = true;
                                            break;

                                        case "<":
                                            opreators.Pop();
                                            integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                                            if (GetLongValueFromString(values.Pop().ToString()) <= integerValue)
                                                tempBoolean = true;
                                            break;

                                        default:
                                            var firstValue = (int)GetLongValueFromString(values.Pop().ToString());
                                            var secondValue = (int)GetLongValueFromString(values.Pop().ToString());

                                            if (secondValue == firstValue)
                                                tempBoolean = true;
                                            break;
                                    }
                                }
                                else
                                {
                                    var firstValue = (int)GetLongValueFromString(values.Pop().ToString());
                                    var secondValue = (int)GetLongValueFromString(values.Pop().ToString());

                                    if (secondValue == firstValue)
                                        tempBoolean = true;
                                }
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
                            else if (opreator.Equals("~"))
                            {
                                integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                                integerValue = ~integerValue;
                                values.Push(integerValue);
                            }
                            else if (opreator.Equals(">"))
                            {
                                if (opreators.Count > 0)
                                {
                                    switch (opreators.Peek())
                                    {
                                        case ">":
                                            opreators.Pop();
                                            integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                                            integerValue = ((int)GetLongValueFromString(values.Pop().ToString()) >> integerValue);
                                            values.Push(integerValue);
                                            break;

                                        default:
                                            integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                                            if (GetLongValueFromString(values.Pop().ToString()) > integerValue)
                                                tempBoolean = true;
                                            break;
                                    }
                                }
                                else
                                {
                                    integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                                    if (GetLongValueFromString(values.Pop().ToString()) > integerValue)
                                        tempBoolean = true;
                                }
                            }
                            else if (opreator.Equals("<"))
                            {
                                if (opreators.Count > 0)
                                {
                                    switch (opreators.Peek())
                                    {
                                        case "<":
                                            opreators.Pop();
                                            integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                                            integerValue = ((int)GetLongValueFromString(values.Pop().ToString()) << integerValue);
                                            values.Push(integerValue);
                                            break;

                                        default:
                                            integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                                            if (GetLongValueFromString(values.Pop().ToString()) < integerValue)
                                                tempBoolean = true;
                                            break;
                                    }
                                }
                                else
                                {
                                    integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                                    if (GetLongValueFromString(values.Pop().ToString()) < integerValue)
                                        tempBoolean = true;
                                }
                            }
                            else if (opreator.Equals("?"))
                            {
                                if (tempBoolean)
                                {
                                    if (values.Count > 0)
                                        return values.Pop();
                                }
                            }
                            else if (opreator.Equals(":"))
                            {
                            }
                        }
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

        private long GetLongValueFromString(string value)
        {
            if (value.StartsWith("0x"))
            {
                value = value.Replace("0x", "");
                return long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            }

            return long.Parse(value); ;
        }

        public async Task<Int64> GetValue()
        {
            return (Int64)await ExecuteFormula(this);
        }
    }
}