using Ghpr.MSTestV2.Utils;

namespace ConsoleAppForDebug
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var path = args[0];
            GhprMSTestV2RunHelper.CreateReportFromFile(path, new EmptyTestDataProvider());
        }
    }
}
