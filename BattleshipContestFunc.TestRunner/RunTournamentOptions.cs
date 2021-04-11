using CommandLine;

namespace BattleshipContestFunc.TestRunner
{

    [Verb("run", HelpText = "Runs a battleship tournament")]
    internal class RunTournamentOptions : BaseOptions
    {
        [Option('g', "games", HelpText = "Number of simultaneous games", Default = 1)]
        public int Games { get; set; } = 1;

        [Option('r', "get-ready-timeout", HelpText = "Timeout for *getReady* API in ms", Default = 15000)]
        public int GetReadyTimeout { get; set; } = 15000;

        [Option('s', "get-shots-timeout", HelpText = "Timeout for *getShots* API in ms", Default = 3000)]
        public int GetShotsTimeout { get; set; } = 15000;
    }
}
