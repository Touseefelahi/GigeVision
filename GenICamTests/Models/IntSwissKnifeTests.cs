using Microsoft.VisualStudio.TestTools.UnitTesting;
using GenICam;
using System;
using System.Collections.Generic;
using System.Text;

namespace GenICam.Tests
{
    [TestClass()]
    public class IntSwissKnifeTests
    {
        [TestMethod()]
        public void ExecuteFormulaTest()
        {
            //string formula = "30*(VAR_PLC_PG0_GRANULARITYFACTOR+1 )*(VAR_PLC_PG0_WIDTH+VAR_PLC_PG0_DELAY+1)";
            //Dictionary<string, IGenRegister> pVaribles = new Dictionary<string, IGenRegister>();

            //pVaribles.Add("VAR_PLC_PG0_GRANULARITYFACTOR", new GenInteger(10));
            //pVaribles.Add("VAR_PLC_PG0_WIDTH", new GenInteger(5));
            //pVaribles.Add("VAR_PLC_PG0_DELAY", new GenInteger(20));
            //IntSwissKnife intSwissKnife = new IntSwissKnife(formula, pfeatures);

            //Assert.AreEqual(8580, Int64.Parse(intSwissKnife.Value.ToString()));
        }
    }
}