using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestsForDebug
{
    [TestClass]
    public class Class1
    {
        [TestMethod]
        public void Test1()
        {
            Console.WriteLine("Some test log...");
        }

        [DataRow(1, 2, 2)]
        [DataRow(1, 2, 3)]
        [DataRow(1, 2, 4)]
        [DataTestMethod]
        public void NewParamTest(int a, int b, int c)
        {
            Assert.AreEqual(a + b, c);
        }
    }
}
