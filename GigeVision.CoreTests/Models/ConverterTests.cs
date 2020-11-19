using Microsoft.VisualStudio.TestTools.UnitTesting;
using GigeVision.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace GigeVision.Core.Models.Tests
{
    [TestClass()]
    public class ConverterTests
    {
        [TestMethod()]
        public void RegisterStringToByteArrayTest()
        {
            var bytes0 = Converter.RegisterStringToByteArray("1245");
            CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x12, 0x45 }, bytes0);
            var bytes1 = Converter.RegisterStringToByteArray("0x1245");
            CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x12, 0x45 }, bytes1);
            var bytes2 = Converter.RegisterStringToByteArray("A045456");
            CollectionAssert.AreEqual(new byte[] { 0x0A, 0x04, 0x54, 0x56 }, bytes2);
            //var bytes3 = Converter.RegisterStringToByteArray(GigeVision.Core.GvcpRegister.CCP.ToString("X"));
            //CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x0A, 0x00 }, bytes3);
            var bytes4 = Converter.RegisterStringToByteArray("9978");
            CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x99, 0x78 }, bytes4);
        }

        [TestMethod()]
        public void StringFormulaToMathematicalExpression()
        {
            Dictionary<string, uint> registers = new Dictionary<string, uint>();
            //var formula = "((varible1/ varible2)+2)*3)";
            var formula = "(VAR_GETMINMAXVERTSIZE & 0xFFFF0000) >> 16";
            registers.Add("VAR_GETMINMAXVERTSIZE", 749600);

            //registers.Add("varible1", 100);
            //registers.Add("varible2", 5);

            //var formula = "(SEL = 0) ? 0x0000B824 : ((SEL = 1) ? 0x0000B82C : (0xFFFFFFFF))";
            //registers.Add("SEL", 0);

            var newFormula = formula.Replace(" ", "");
            int counter = 0;
            foreach (var character in formula.ToCharArray())
            {
                List<char> opreations = new List<char> { '(', '+', '-', '/', '*', '=', '?', ':', ')', '&', '>' };
                if (opreations.Where(x => x == character).Count() > 0)
                {
                    if (counter > 0)
                        newFormula = newFormula.Replace($"{character}", $"{character}");
                    else
                        newFormula = newFormula.Replace($"{character}", $" {character} ");
                    counter++;
                }
                else
                {
                    counter = 0;
                }
            }
            foreach (var word in newFormula.Split())
            {
                foreach (var register in registers)
                {
                    if (register.Key.Equals(word))
                        newFormula = newFormula.Replace(word, $"{register.Value}");
                }
            }

            //object can be either double for register value  or string for register address
            var r = Evaluate(newFormula);
        }

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
                    else if (word.Equals("?")) opreators.Push(word);
                    else if (word.Equals(":")) opreators.Push(word);
                    else if (word.Equals("&"))
                        opreators.Push(word);
                    else if (word.Equals(">"))
                        opreators.Push(word);
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
                                if (value == values.Pop()) tempBoolean = true;
                            }
                            else if (opreator.Equals("&"))
                            {
                                var byte1 = Int32.Parse(value.ToString());
                                var byte2 = Int32.Parse(tempAddress.Substring(2), System.Globalization.NumberStyles.HexNumber);
                                value = (byte1 & byte2);
                                tempAddress = string.Empty;
                            }
                            else if (opreator.Equals(">"))
                            {
                                if (opreators.Peek().Equals(">"))
                                {
                                    opreators.Pop();
                                    count--;
                                    value = ((int)values.Pop() >> (byte)value);
                                }
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