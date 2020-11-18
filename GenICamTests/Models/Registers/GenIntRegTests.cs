using Microsoft.VisualStudio.TestTools.UnitTesting;
using GenICam;
using System;
using System.Collections.Generic;
using System.Text;

namespace GenICam.Tests
{
    [TestClass()]
    public class GenIntRegTests
    {
        [TestMethod()]
        public void GetTest()
        {
            GenInteger genInteger = new GenInteger(7);

            var value = genInteger.GetValue();
        }
    }
}