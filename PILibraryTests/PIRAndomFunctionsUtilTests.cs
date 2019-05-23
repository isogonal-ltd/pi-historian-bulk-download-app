using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PILibrary;

namespace PILibraryTests
{
    [TestClass]
    public class PIRandomFunctionsUtilTests
    {
        [TestMethod]
        public void TestDateTimeToDatePath()
        {
            DateTime time = new DateTime(2018, 10, 8);
            Assert.AreEqual(10, time.Month);
            Assert.AreEqual(8, time.Day);
            Assert.AreEqual("2018", PIRandomFunctionsUtil.DateTimeToDatePath(time, PIRandomFunctionsUtil.TimeResolution.Year), "Year mod 1");
            Assert.AreEqual("2018/10", PIRandomFunctionsUtil.DateTimeToDatePath(time, PIRandomFunctionsUtil.TimeResolution.Month), "Month only");
            Assert.AreEqual("2018/10/08", PIRandomFunctionsUtil.DateTimeToDatePath(time, PIRandomFunctionsUtil.TimeResolution.Day), "Day only");
            Assert.AreEqual("2018", PIRandomFunctionsUtil.DateTimeToDatePath(time, PIRandomFunctionsUtil.TimeResolution.Year, 2), "Year mod 2");
            Assert.AreEqual("2015", PIRandomFunctionsUtil.DateTimeToDatePath(time, PIRandomFunctionsUtil.TimeResolution.Year, 5), "Year mod 5");
            Assert.AreEqual("2010", PIRandomFunctionsUtil.DateTimeToDatePath(time, PIRandomFunctionsUtil.TimeResolution.Year, 10), "Year mod 10");
        }
    }
}
