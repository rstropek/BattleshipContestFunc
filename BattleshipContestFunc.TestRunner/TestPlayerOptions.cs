using CommandLine;

namespace BattleshipContestFunc.TestRunner
{

    [Verb("test", HelpText = "Tests a player by sending a *getReady* request.")]
    internal class TestPlayerOptions : BaseOptions
    {
        [Option('t', "timeout", HelpText = "Timeout in ms", Default = 15000)]
        public int Timeout { get; set; } = 15000;
    }
}
