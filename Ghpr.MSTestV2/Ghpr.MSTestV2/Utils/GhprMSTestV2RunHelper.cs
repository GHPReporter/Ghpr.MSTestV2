using System;
using System.Collections.Generic;
using System.Linq;
using Ghpr.Core.Common;
using Ghpr.Core.Enums;
using Ghpr.Core.Factories;
using Ghpr.Core.Interfaces;

namespace Ghpr.MSTestV2.Utils
{
    public class GhprMSTestV2RunHelper
    {
        public static void CreateReportFromFile(string path, ITestDataProvider dataProvider)
        {
            var reporter = ReporterFactory.Build(TestingFramework.MSTestV2, dataProvider, path);
            try
            {
                var testRuns = GetTestRunsListFromFile(path);
                foreach (var ghprTestCase in testRuns.Where(t => t.GhprTestScreenshots.Any()))
                {
                    foreach (var screenshot in ghprTestCase.GhprTestScreenshots)
                    {
                        reporter.DataWriterService.SaveScreenshot(screenshot);
                    }
                }
                reporter.GenerateFullReport(testRuns
                    .Select(tr => new KeyValuePair<TestRunDto, TestOutputDto>(tr.GhprTestRun, tr.GhprTestOutput)).ToList());
                reporter.CleanUpJob();
                reporter.TearDown();
            }
            catch (Exception ex)
            {
                reporter.Logger.Exception("Exception in CreateReportFromFile", ex);
            }
        }

        public static List<GhprTestCase> GetTestRunsListFromFile(string path)
        {
            var reader = new TrxReader(path);
            var testRuns = reader.GetTestRuns();
            return testRuns;
        }
    }
}