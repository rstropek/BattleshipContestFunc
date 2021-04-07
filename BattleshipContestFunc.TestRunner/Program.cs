using CommandLine;
using System;
using System.Threading.Tasks;

namespace BattleshipContestFunc.TestRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<TestPlayerOptions>(args)
                .MapResult(
                  (TestPlayerOptions options) => TestPlayer(options),
                  errors => 1);
        }

        private static int TestPlayer(TestPlayerOptions options)
        {
            return 0;
        }
    }
}
