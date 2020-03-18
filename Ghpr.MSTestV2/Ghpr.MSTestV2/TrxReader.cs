using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Ghpr.Core.Common;
using Ghpr.Core.Extensions;
using Ghpr.MSTestV2.Utils;

namespace Ghpr.MSTestV2
{
    public class TrxReader
    {
        private readonly XmlDocument _xml;
        private readonly string _trxFullPath;

        public TrxReader(string fullPath)
        {
            _xml = XmlExtensions.GetDoc(fullPath);
            _trxFullPath = fullPath;
        }

        public string GetRunGuid()
        {
            return _xml.GetNode("TestRun").GetAttrVal("id");
        }

        public List<GhprTestCase> GetTestRuns()
        {
            var testRuns = new List<GhprTestCase>();
            var deploymentFolder = _xml.GetNode("TestSettings")?.GetNode("Deployment")?.GetAttrVal("runDeploymentRoot");
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
                    var executionId = utr.GetAttrVal("executionId");
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
                    var testGuid = testFullName.ToMd5HashGuid();
                    var testInfo = new ItemInfoDto
                    {
                        Start = start,
                        Finish = finish,
                        Guid = testGuid
                    };
                    var result = utr.GetAttrVal("outcome");
                    var outputNode = utr.GetNode("Output");
                    var output = outputNode?.GetNode("StdOut")?.InnerText ?? "";
                    var msg = outputNode?.GetNode("ErrorInfo")?.GetNode("Message")?.InnerText ?? "";
                    var sTrace = outputNode?.GetNode("ErrorInfo")?.GetNode("StackTrace")?.InnerText ?? "";
                    
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

                    var testScreenshots = new List<TestScreenshotDto>();
                    var resFiles = utr.GetNode("ResultFiles")?.GetNodesList("ResultFile");
                    foreach (var resFile in resFiles)
                    {
                        var relativePath = resFile.GetAttrVal("path");
                        var fullResFilePath = Path.Combine(
                            Path.GetDirectoryName(_trxFullPath), deploymentFolder, "In", executionId, relativePath);
                        if (File.Exists(fullResFilePath))
                        {
                            try
                            {
                                var ext = Path.GetExtension(fullResFilePath);
                                if (new[] {"png", "jpg", "jpeg", "bmp"}.Contains(ext.Replace(".","").ToLower()))
                                {
                                    var fileInfo = new FileInfo(fullResFilePath);
                                    var bytes = File.ReadAllBytes(fullResFilePath);
                                    var base64 = Convert.ToBase64String(bytes);
                                    var screenInfo = new SimpleItemInfoDto
                                    {
                                        Date = fileInfo.CreationTimeUtc,
                                        ItemName = ""
                                    };
                                    var testScreenshotDto = new TestScreenshotDto
                                    {
                                        Format = ext.Replace(".", ""),
                                        TestGuid = testGuid,
                                        TestScreenshotInfo = screenInfo,
                                        Base64Data = base64
                                    };
                                    testScreenshots.Add(testScreenshotDto);
                                    testRun.Screenshots.Add(screenInfo);
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"Error when trying to add test attachment: {e.Message}{Environment.NewLine}" +
                                                  $"{e.StackTrace}{Environment.NewLine}" +
                                                  $"The test XML node is:{Environment.NewLine}" +
                                                  $"{utr.OuterXml}" + 
                                                  $"The file path is:{Environment.NewLine}" +
                                                  $"{fullResFilePath}");
                            }
                        }
                    }
                    
                    var ghprTestCase = new GhprTestCase
                    {
                        Id = testGuid.ToString(),
                        ParentId = "",
                        GhprTestRun = testRun,
                        GhprTestOutput = testOutput,
                        GhprTestScreenshots = testScreenshots
                    };

                    testRuns.Add(ghprTestCase);

                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error when trying to parse the test: {e.Message}{Environment.NewLine}" +
                                      $"{e.StackTrace}{Environment.NewLine}" +
                                      $"The test XML node is:{Environment.NewLine}" +
                                      $"{utr.OuterXml}");
                }
            }

            return testRuns;
        }
    }
}
