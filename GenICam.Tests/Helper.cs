using System.Xml;
using Xunit;
using GenICam;
using GigeVision.Core;
using GigeVision.Core.Models;
using System.Collections.Generic;
using org.mariuszgromada.math.mxparser;
using System.Linq;
using System.IO;
using System;
using GigeVision.Core.Services;

namespace GenICam.Tests
{
    public class Helper
    {
        [Fact]
        public async void ReadAllRegisters()
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load("Imperx.xml");
            var genPort = new GenPort(new Gvcp());
            var xmlHelper = new XmlHelper(xmlDocument, genPort);
            await xmlHelper.LoadUp(true);

            Assert.NotEmpty(xmlHelper.CategoryDictionary);
        }


        [Theory]
        [InlineData("((0x00000001=1)||(0x00000000=1))?(0x00000600 +12):(0x00000800)")]
        [InlineData("(0x00000600+(0x00000000*8))")]
        [InlineData("(16=0)? 1: ( (0=1)?2:((0=2)? 3 :( (0=4)?4:((0=8)?5:((16=16)?6:((0=32)?7:8))))))")]
        public void MathParser(string formula)
        {
            var expectedValue = 6; 
            formula = MathParserHelper.FormatExpression(formula);
            var actualValue = MathParserHelper.CalculateExpression(formula);
            Assert.Equal(expectedValue, actualValue);
        }

        [Theory]
        [InlineData("(8))")]
        [InlineData("((8)")]
        public void GetBracket(string formula)
        {
            var expected = "(8)";
            var actual  = MathParserHelper.GetBracketed(formula);
            Assert.Equal(expected, actual);
        }
    }
}