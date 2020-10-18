using GigeVision.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public double? Value { get; set; }

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
        public IntSwissKnife(IGvcp gvcp, string formula, object pVarible, double? value = null)
        {
            Gvcp = gvcp;
            VariableParameter = pVarible;
            Formula = formula;
            //Prepare Expression

            Formula = Formula.Replace(" ", "");
            List<char> opreations = new List<char> { '(', '+', '-', '/', '*', '=', '?', ':', ')' };
            foreach (var character in opreations)
            {
                if (opreations.Where(x => x == character).Count() > 0)
                {
                    Formula = Formula.Replace($"{character}", $" {character} ");
                }
            }

            ExecuteFormula(this).ConfigureAwait(false);
        }

        //private async Task<object> ReadRegisterParameter(object pVariable)
        //{
        //    double? value = null;

        //    if (pVariable is IntSwissKnife intSwissKnife)
        //    {
        //        var formulaValue = await ExecuteFormula(intSwissKnife);
        //        if (formulaValue is string addressValue)
        //            value = (await ReadRegisterAsync(addressValue)).RegisterValue;
        //        if (formulaValue is double doubleValue)
        //            value = doubleValue;
        //    }

        //    if (pVariable is CameraRegister cameraRegister)
        //        value = (await ReadRegisterAsync(cameraRegister.Address)).RegisterValue;

        //    return value;
        //}

        //private async Task ReadIntSwissKnifeVariables(IntSwissKnife intSwissKnife)
        //{
        //    if (intSwissKnife.VariableParameter is Dictionary<string, string> pVariablesCameraRegister)
        //    {
        //        foreach (var pVariable in pVariablesCameraRegister)
        //        {
        //            RegistersDictionary.Where(x => x.Value.Register != null).Where(x => x.Value.Register.Address == pVariable.Value).First().Value.Register.Value = (await ReadRegisterAsync(pVariable.Value)).RegisterValue;
        //        }
        //    }
        //    else if (intSwissKnife.VariableParameter is Dictionary<string, IntSwissKnife> pVariableIntSwissKnifeDictionary)
        //    {
        //        foreach (var item in pVariableIntSwissKnifeDictionary.Values)
        //        {
        //            await ReadIntSwissKnifeVariables(item);
        //        }
        //    }
        //    else if (intSwissKnife.VariableParameter is IntSwissKnife pVariableIntSwissKnife)
        //    {
        //        await ReadIntSwissKnifeVariables(pVariableIntSwissKnife);
        //    }
        //}

        /// <summary>
        /// this method calculates the formula and returns the result
        /// </summary>
        /// <param name="intSwissKnife"></param>
        /// <returns></returns>
        public async Task<object> ExecuteFormula(IntSwissKnife intSwissKnife)
        {
            if (intSwissKnife.VariableParameter is Dictionary<string, string> pVariableCameraAddress)
            {
                foreach (var register in pVariableCameraAddress)
                {
                    string tempWord = string.Empty;
                    string tempValue = string.Empty;
                    foreach (var word in intSwissKnife.Formula.Split())
                    {
                        if (register.Key.Equals(word) && !tempWord.Equals(word))
                        {
                            tempWord = word;
                            tempValue = $"{(Gvcp.ReadRegisterAsync(register.Value).Result.RegisterValue)}";
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
                    pVarible.Value.Value = (await ExecuteFormula(pVarible.Value)) as double?;
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
                            if (pVarible.Value.Register is null)
                            {
                                if (pVarible.Value.TypeValue is IntegerRegister integerRegister)
                                    intSwissKnife.Formula = intSwissKnife.Formula.Replace(word, $"{integerRegister.Value}");
                            }
                            else if (pVarible.Value.Register.AddressParameter != null)
                                intSwissKnife.Formula = intSwissKnife.Formula.Replace(word, $"{pVarible.Value.Register.AddressParameter.Value}");
                            else if (!tempWord.Equals(word))
                            {
                                tempWord = word;
                                tempValue = $"{(Gvcp.ReadRegisterAsync(pVarible.Value.Register.Address).Result.RegisterValue)}";
                                intSwissKnife.Formula = intSwissKnife.Formula.Replace(word, tempValue);
                            }
                            else if (word.Equals(tempWord) && tempValue != string.Empty)
                                intSwissKnife.Formula = intSwissKnife.Formula.Replace(word, tempValue);
                        }
                    }
                }
            }

            var value = Evaluate(intSwissKnife.Formula);

            if (value is string pAddressValue)
                Value = (await Gvcp.ReadRegisterAsync(pAddressValue)).RegisterValue;

            if (value is double doubleValue)
                Value = doubleValue;

            intSwissKnife.Value = Value;
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
            Stack<double> values = new Stack<double>();
            string number = string.Empty;
            double tempNumber = 0;
            bool tempBoolean = false;
            string tempAddress = string.Empty;

            foreach (var word in expression.Split())
            {
                if (double.TryParse(word, out tempNumber))
                {
                    number += word;
                }
                else if (tempBoolean)
                {
                    if (word.StartsWith("0x") && word.Length == 10)
                        return word;
                }
                else if (word.StartsWith("0x") && word.Length == 10)
                    tempAddress = word;
                else
                {
                    if (number != string.Empty)
                    {
                        values.Push(double.Parse(number));
                        number = string.Empty;
                    }

                    if (word.Equals("(")) { }
                    else if (word.Equals("+")) opreators.Push(word);
                    else if (word.Equals("-")) opreators.Push(word);
                    else if (word.Equals("*")) opreators.Push(word);
                    else if (word.Equals("/")) opreators.Push(word);
                    else if (word.Equals("=")) opreators.Push(word);
                    else if (word.Equals(")"))
                    {
                        int count = opreators.Count;
                        while (count > 0)
                        {
                            string opreator = opreators.Pop();
                            double value = values.Pop();
                            if (opreator.Equals("+")) value = values.Pop() + value;
                            else if (opreator.Equals("-")) value = values.Pop() - value;
                            else if (opreator.Equals("*")) value = values.Pop() * value;
                            else if (opreator.Equals("/")) value = values.Pop() / value;
                            else if (opreator.Equals("="))
                            {
                                if (value == values.Pop())
                                    tempBoolean = true;
                            }

                            values.Push(value);

                            count--;
                        }
                    }
                }
            }
            if (tempAddress != string.Empty)
                return tempAddress;

            return values.Pop();
        }
    }
}