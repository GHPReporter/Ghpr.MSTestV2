using System;
using Ghpr.Core.Interfaces;

namespace ConsoleAppForDebug
{
    public class EmptyTestDataProvider : ITestDataProvider
    {
        public Guid GetCurrentTestRunGuid()
        {
            throw new NotImplementedException();
        }

        public string GetCurrentTestRunFullName()
        {
            throw new NotImplementedException();
        }
    }
}