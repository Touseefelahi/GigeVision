using Microsoft.VisualStudio.TestTools.UnitTesting;
using GigeVision.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GigeVision.Core.Enums;
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
            var formula = "((varible1/ varible2)/varible1+8)*8";
            Dictionary<string, uint> registers = new Dictionary<string, uint>();
            registers.Add("varible1", 5);
            registers.Add("varible2", 5);

            foreach (var character in formula.ToCharArray())
            {
                List<char> opreations = new List<char> { '(', '+', '-', '/', '*', ')' };
                if (opreations.Where(x => x == character).Count() > 0)
                {
                    formula = formula.Replace($"{character}", $" {character} ");
                }
            }

            foreach (var word in formula.Split())
            {
                foreach (var register in registers)
                {
                    if (register.Key.Equals(word))
                        formula = formula.Replace(word, $"{register.Value}");
                }
            }

            var r = Evaluate(formula);
        }

        private double Evaluate(string expression)
        {
            expression = expression.Replace(" ", "");
            expression = "(" + expression + ")";
            Stack<string> opreators = new Stack<string>();
            Stack<double> values = new Stack<double>();

            for (int i = 0; i < expression.Length; i++)
            {
                string letter = expression.Substring(i, 1);
                if (letter.Equals("(")) { }
                else if (letter.Equals("+")) opreators.Push(letter);
                else if (letter.Equals("-")) opreators.Push(letter);
                else if (letter.Equals("*")) opreators.Push(letter);
                else if (letter.Equals("/")) opreators.Push(letter);
                else if (letter.Equals("sqrt")) opreators.Push(letter);
                else if (letter.Equals(")"))
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
                        else if (opreator.Equals("sqrt")) value = Math.Sqrt(value);
                        values.Push(value);

                        count--;
                    }
                }
                else values.Push(double.Parse(letter));
            }
            return values.Pop();
        }
    }
}