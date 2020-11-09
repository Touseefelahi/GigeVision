using GigeVision.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GigeVision.Core.Models
{
    /// <summary>
    /// this is a mathematical calss for register parameter computations
    /// </summary>
    public class IntSwissKnife
    {
        /// <summary>
        /// Math Variable Parameter
        /// </summary>
        public object VariableParameter { get; set; }

        /// <summary>
        /// Formula Expression
        /// </summary>
        public string Formula { get; private set; }

        /// <summary>
        /// Formula Result
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Gvcp is here for reading variable parameter (registers)
        /// </summary>
        private IGvcp Gvcp { get; }

        /// <summary>
        /// Main Method that calculate the given formula
        /// </summary>
        /// <param name="gvcp"></param>
        /// <param name="formula"></param>
        /// <param name="pVarible"></param>
        /// <param name="value"></param>
        public IntSwissKnife(IGvcp gvcp, string formula, object pVarible, object value = null)
        {
            Gvcp = gvcp;
            VariableParameter = pVarible;
            Formula = formula;
            //Prepare Expression

            if (Formula.Equals("(VAR_PLC_INTERRUPT_FIFO0_OFFSET26 & 0xFF00) >> 8"))
            {
                var x = 0;
            }

            Formula = Formula.Replace(" ", "");
            List<char> opreations = new List<char> { '(', '+', '-', '/', '*', '=', '?', ':', ')', '>', '<', '&', '|' };
            foreach (var character in opreations)
            {
                if (opreations.Where(x => x == character).Count() > 0)
                {
                    Formula = Formula.Replace($"{character}", $" {character} ");
                }
            }

            ExecuteFormula(this).ConfigureAwait(false);
        }

        /// <summary>
        /// this method calculates the formula and returns the result
        /// </summary>
        /// <param name="intSwissKnife"></param>
        /// <returns></returns>
        public async Task<object> ExecuteFormula(IntSwissKnife intSwissKnife)
        {
            if (intSwissKnife.VariableParameter is Dictionary<string, string> pVariableCameraAddress)
            {
                foreach (KeyValuePair<string, string> register in pVariableCameraAddress)
                {
                    string tempWord = string.Empty;
                    string tempValue = string.Empty;
                    foreach (var word in intSwissKnife.Formula.Split())
                    {
                        if (register.Key.Equals(word) && !tempWord.Equals(word))
                        {
                            tempWord = word;

                            var reply = await Gvcp.ReadRegisterAsync(register.Value);
                            if (reply.Status != Enums.GvcpStatus.GEV_STATUS_SUCCESS)
                                continue;

                            tempValue = $"{reply.RegisterValue}";

                            intSwissKnife.Formula = intSwissKnife.Formula.Replace(word, tempValue);
                        }
                        else if (word.Equals(tempWord) && tempValue != string.Empty)
                        {
                            intSwissKnife.Formula = intSwissKnife.Formula.Replace(word, tempValue);
                        }
                    }
                }
            }
            else if (intSwissKnife.VariableParameter is Dictionary<string, IntSwissKnife> pVariableIntSwissKnifeDictionary)
            {
                foreach (var pVarible in pVariableIntSwissKnifeDictionary)
                {
                    pVarible.Value.Value = await ExecuteFormula(pVarible.Value);
                    foreach (var word in intSwissKnife.Formula.Split())
                    {
                        if (pVarible.Key.Equals(word))
                            intSwissKnife.Formula = intSwissKnife.Formula.Replace(word, $"{pVarible.Value.Value}");
                    }
                }
            }
            else if (intSwissKnife.VariableParameter is Dictionary<string, CameraRegisterContainer> pVariableCameraRegisterContainerDictionary)
            {
                foreach (var pVarible in pVariableCameraRegisterContainerDictionary)
                {
                    string tempWord = string.Empty;
                    string tempValue = string.Empty;

                    foreach (var word in intSwissKnife.Formula.Split())
                    {
                        if (pVarible.Key.Equals(word))
                        {
                            if (pVarible.Value.Value is uint uintValue)
                                intSwissKnife.Formula = intSwissKnife.Formula.Replace(word, $"{uintValue}");
                            else if (pVarible.Value.TypeValue is FloatRegister floatRegister)
                                intSwissKnife.Formula = intSwissKnife.Formula.Replace(word, $"{floatRegister.ValueParameter.Value}");
                            else if (pVarible.Value.Register != null)
                            {
                                if (pVarible.Value.Register.Address != null)
                                {
                                    if (pVarible.Value.Register.Address is string pAddress)
                                    {
                                        var reply = await Gvcp.ReadRegisterAsync(pAddress);
                                        if (reply.Status != Enums.GvcpStatus.GEV_STATUS_SUCCESS)
                                            continue;

                                        pVarible.Value.Value = reply.RegisterValue;
                                    }

                                    intSwissKnife.Formula = intSwissKnife.Formula.Replace(word, $"{pVarible.Value.Value}");
                                }
                                else if (pVarible.Value.Register.AddressParameter != null)
                                {
                                    if (pVarible.Value.Register.AddressParameter.Value is string pAddress)
                                    {
                                        var reply = await Gvcp.ReadRegisterAsync(pAddress);
                                        if (reply.Status != Enums.GvcpStatus.GEV_STATUS_SUCCESS)
                                            continue;

                                        pVarible.Value.Value = reply.RegisterValue;
                                    }

                                    intSwissKnife.Formula = intSwissKnife.Formula.Replace(word, $"{pVarible.Value.Value}");
                                }
                            }
                            else if (!tempWord.Equals(word))
                            {
                                tempWord = word;

                                if (pVarible.Value.Register != null)
                                {
                                    var reply = await Gvcp.ReadRegisterAsync(pVarible.Value.Register.Address);
                                    if (reply.Status != Enums.GvcpStatus.GEV_STATUS_SUCCESS)
                                        continue;

                                    tempValue = $"{reply.RegisterValue}";
                                }
                                else if (pVarible.Value.Value != null)
                                    tempValue = pVarible.Value.Value.ToString();

                                intSwissKnife.Formula = intSwissKnife.Formula.Replace(word, tempValue);
                            }
                            else if (word.Equals(tempWord) && tempValue != string.Empty)
                                intSwissKnife.Formula = intSwissKnife.Formula.Replace(word, tempValue);
                        }
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
        private object Evaluate(string expression)
        {
            expression = "( " + expression + " )";
            Stack<string> opreators = new Stack<string>();
            Stack<object> values = new Stack<object>();
            bool tempBoolean = false;
            string tempAddress = string.Empty;
            int integerValue = 0;

            foreach (var word in expression.Split())
            {
                if (word.StartsWith("0x"))
                    values.Push(word);
                else if (double.TryParse(word, out double tempNumber))
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
                    else if (word.Equals(")"))
                    {
                        int count = opreators.Count;
                        while (count > 0)
                        {
                            string opreator = opreators.Pop();
                            double value = 0;
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
                                value = (double)values.Pop();
                                value = (double)values.Pop() * value;
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

                                if (firstValue == secondValue)
                                    tempBoolean = true;
                            }
                            else if (opreator.Equals("&"))
                            {
                                var byte2 = (int)GetLongValueFromString(values.Pop().ToString());
                                var byte1 = (int)GetLongValueFromString(values.Pop().ToString());
                                integerValue = (byte1 & byte2);
                                values.Push(integerValue);
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
                            else if (opreator.Equals(">"))
                            {
                                if (opreators.Peek().Equals(">"))
                                {
                                    opreators.Pop();
                                    count--;
                                    integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                                    integerValue = ((int)GetLongValueFromString(values.Pop().ToString()) >> integerValue);
                                    values.Push(integerValue);
                                }
                            }
                            else if (opreator.Equals("<"))
                            {
                                if (opreators.Peek().Equals("<"))
                                {
                                    opreators.Pop();
                                    count--;
                                    integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                                    integerValue = ((int)GetLongValueFromString(values.Pop().ToString()) << integerValue);
                                    values.Push(integerValue);
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

                            count--;
                        }
                    }
                }
            }
            if (values.Count > 0)
                return values.Pop();

            return null;
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
    }
}