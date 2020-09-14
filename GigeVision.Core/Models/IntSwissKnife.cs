using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GigeVision.Core.Models
{
    public class IntSwissKnife
    {
        public object PVarible { get; private set; }
        public string Formula { get; private set; }
        public uint Value { get; private set; }

        public IntSwissKnife(string formula, uint value, object pVarible)
        {
            PVarible = pVarible;

            //Prepare Expression
            foreach (var character in Formula.ToCharArray())
            {
                List<char> opreations = new List<char> { '(', '+', '-', '/', '*', ')' };
                if (opreations.Where(x => x == character).Count() > 0)
                {
                    Formula = Formula.Replace($"{character}", $" {character} ");
                }
            }

            if (PVarible is Dictionary<string, CameraRegister> cameraRegisters)
            {
                foreach (var word in Formula.Split())
                {
                    foreach (var register in cameraRegisters)
                    {
                        if (register.Key.Equals(word))
                            Formula = Formula.Replace(word, $"{register.Value}");
                    }
                }
            }
            else if (PVarible is IntSwissKnife intSwissKnife)
            {
                if (intSwissKnife.PVarible is Dictionary<string, CameraRegister> cameraRegistersDictionary)
                {
                    foreach (var word in Formula.Split())
                    {
                        foreach (var register in cameraRegistersDictionary)
                        {
                            if (register.Key.Equals(word))
                                Formula = Formula.Replace(word, $"{register.Value}");
                        }
                    }
                }
            }

            //Execute
            Value = Evaluate(Formula);
        }

        private uint Evaluate(string expression)
        {
            expression = expression.Replace(" ", "");
            expression = "(" + expression + ")";
            Stack<string> opreators = new Stack<string>();
            Stack<uint> values = new Stack<uint>();

            for (int i = 0; i < expression.Length; i++)
            {
                string letter = expression.Substring(i, 1);
                if (letter.Equals("(")) { }
                else if (letter.Equals("+")) opreators.Push(letter);
                else if (letter.Equals("-")) opreators.Push(letter);
                else if (letter.Equals("*")) opreators.Push(letter);
                else if (letter.Equals("/")) opreators.Push(letter);
                else if (letter.Equals(")"))
                {
                    int count = opreators.Count;
                    while (count > 0)
                    {
                        string opreator = opreators.Pop();
                        uint value = values.Pop();
                        if (opreator.Equals("+")) value = values.Pop() + value;
                        else if (opreator.Equals("-")) value = values.Pop() - value;
                        else if (opreator.Equals("*")) value = values.Pop() * value;
                        else if (opreator.Equals("/")) value = values.Pop() / value;
                        values.Push(value);

                        count--;
                    }
                }
                else values.Push(uint.Parse(letter));
            }
            return values.Pop();
        }
    }
}