using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Ghpr.Core.Common;
using Ghpr.Core.Extensions;

namespace Ghpr.MSTestV2
{
    public class TrxReader
    {
        private readonly XmlDocument _xml;

        public TrxReader(string fullPath)
        {
            _xml = XmlExtensions.GetDoc(fullPath);
        }

        public string GetRunGuid()
        {
            return _xml.GetNode("TestRun").GetAttrVal("id");
        }

        public List<KeyValuePair<TestRunDto, TestOutputDto>> GetTestRuns()
        {
            var testRuns = new List<KeyValuePair<TestRunDto, TestOutputDto>>();
            var utrs = _xml.GetNodesList("UnitTestResult");
            var uts = _xml.GetNode("TestDefinitions")?.GetNodesList("UnitTest");

            if (utrs == null)
            {
                Console.WriteLine("No tests found!");
                return testRuns;
            }

            foreach (var utr in utrs)
            {
                try
                {
                    var start = utr.GetDateTimeVal("startTime");
                    var finish = utr.GetDateTimeVal("endTime");
                    var internalTestGuid = utr.GetAttrVal("testId") ?? Guid.NewGuid().ToString();

                    var testName = utr.GetAttrVal("testName");
                    var ut = uts?.FirstOrDefault(node => (node.GetAttrVal("id") ?? "").Equals(internalTestGuid));

                    if (utr.FirstChild != null && utr.FirstChild.Name.Equals("InnerResults"))
                    {
                        continue;
                    }

                    var tm = ut?.GetNode("TestMethod");
                    var testDesc = ut?.GetNode("Description")?.InnerText;
                    var testFullName = (tm?.GetAttrVal("className") ?? "").Split(',')[0] + "." + testName;
                    var testInfo = new ItemInfoDto
                    {
                        Start = start,
                        Finish = finish,
                        Guid = testFullName.ToMd5HashGuid()
                    };
                    var result = utr.GetAttrVal("outcome");
                    var output = utr.GetNode("Output")?.GetNode("StdOut")?.InnerText ?? "";
                    var msg = utr.GetNode("Output")?.GetNode("ErrorInfo")?.GetNode("Message")?.InnerText ?? "";
                    var sTrace = utr.GetNode("Output")?.GetNode("ErrorInfo")?.GetNode("StackTrace")?.InnerText ?? "";

                    var testOutputInfo = new SimpleItemInfoDto
                    {
                        Date = finish,
                        ItemName = "Test output"
                    };

                    var testRun = new TestRunDto
                    {
                        TestInfo = testInfo,
                        Name = testName,
                        Description = testDesc,
                        FullName = testFullName,
                        Result = result,
                        Output = testOutputInfo,
                        TestMessage = msg,
                        TestStackTrace = sTrace
                    };

                    var testOutput = new TestOutputDto
                    {
                        TestOutputInfo = testOutputInfo,
                        Output = output,
                        SuiteOutput = ""
                    };

                    testRuns.Add(new KeyValuePair<TestRunDto, TestOutputDto>(testRun, testOutput));

                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error when trying to parse the test: {e.Message}{Environment.NewLine}" +
                                      $"{e.StackTrace}{Environment.NewLine}" +
                                      $"The test XML node is:{Environment.NewLine}" +
                                      $"{utr.OuterXml}");
                    throw;
                }
            }

            return testRuns;
        }
    }
}
