using System;
using System.Collections.Generic;
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
                reporter.GenerateFullReport(testRuns);
                reporter.CleanUpJob();
                reporter.TearDown();
            }
            catch (Exception ex)
            {
                reporter.Logger.Exception("Exception in CreateReportFromFile", ex);
            }
        }

        public static List<KeyValuePair<TestRunDto, TestOutputDto>> GetTestRunsListFromFile(string path)
        {
            var reader = new TrxReader(path);
            var testRuns = reader.GetTestRuns();
            return testRuns;
        }
    }
}