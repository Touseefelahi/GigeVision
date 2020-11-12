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
    public class IntSwissKnife : IPRegister
    {
        /// <summary>
        /// Math Variable Parameter
        /// </summary>
        public Dictionary<string, IPRegister> PVariables { get; set; }

        /// <summary>
        /// Formula Expression
        /// </summary>
        public string Formula { get; private set; }

        /// <summary>
        /// Formula Result
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Gvcp is here for reading variable parameter (registers)
        /// </summary>
        private IGenPort GenPort { get; }

        /// <summary>
        /// Main Method that calculate the given formula
        /// </summary>
        /// <param name="gvcp"></param>
        /// <param name="formula"></param>
        /// <param name="pVarible"></param>
        /// <param name="value"></param>
        public IntSwissKnife(string formula, Dictionary<string, IPRegister> pVaribles)
        {
            PVariables = pVaribles;
            Formula = formula;

            //Prepare Expression
            Formula = Formula.Replace(" ", "");
            List<char> opreations = new List<char> { '(', '+', '-', '/', '*', '=', '?', ':', ')', '>', '<', '&', '|', '^', '~', '%' };

            foreach (var character in opreations)
                if (opreations.Where(x => x == character).Count() > 0)
                    Formula = Formula.Replace($"{character}", $" {character} ");

            //ExecuteFormula(this).ConfigureAwait(false);
        }

        /// <summary>
        /// this method calculates the formula and returns the result
        /// </summary>
        /// <param name="intSwissKnife"></param>
        /// <returns></returns>
        public async Task<object> ExecuteFormula(IntSwissKnife intSwissKnife)
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
                            value = integer.GetValue().ToString();
                        else if (pVariable.Value is GenIntReg intReg)
                        {
                            //ToDo: Implement read register value
                            value = intReg.GetValue().ToString();
                        }
                        if (value == "")
                            throw new Exception("Failed to read register value", new InvalidDataException());

                        intSwissKnife.Formula = intSwissKnife.Formula.Replace(word, value);
                        break;
                    }
                }
            }

            try
            {
                var value = Evaluate(intSwissKnife.Formula);

                Value = value;
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
                if (word != null)
                {
                    if (word.StartsWith("0x"))
                        values.Push(Int64.Parse(word.Substring(2), System.Globalization.NumberStyles.HexNumber));
                    else if (Int64.TryParse(word, out Int64 tempNumber))
                        values.Push(tempNumber);
                }
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
                                var firstValue = (int)GetLongValueFromString(values.Pop().ToString());
                                var secondValue = (int)GetLongValueFromString(values.Pop().ToString());

                                if (secondValue == firstValue)
                                    tempBoolean = true;
                            }
                            else if (opreator.Equals("&"))
                            {
                                if (values.Count > 2)
                                {
                                    var byte2 = (int)GetLongValueFromString(values.Pop().ToString());
                                    var byte1 = (int)GetLongValueFromString(values.Pop().ToString());
                                    integerValue = (byte1 & byte2);
                                    values.Push(integerValue);
                                }
                            }
                            else if (opreator.Equals("|"))
                            {
                                if (values.Count > 2)
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

                                        case "=":
                                            opreators.Pop();
                                            integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                                            if (GetLongValueFromString(values.Pop().ToString()) >= integerValue)
                                                tempBoolean = true;
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

                                        case "=":
                                            opreators.Pop();
                                            integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                                            if (GetLongValueFromString(values.Pop().ToString()) <= integerValue)
                                                tempBoolean = true;
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

        public long GetValue()
        {
            return (Int64)Value;
        }
    }
}