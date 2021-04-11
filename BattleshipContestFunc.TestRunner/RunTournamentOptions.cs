using CommandLine;

namespace BattleshipContestFunc.TestRunner
{

    [Verb("run", HelpText = "Runs a battleship tournament")]
    internal class RunTournamentOptions
    {
        [Option('a', "api-url", HelpText = "Web API URL", Default = "http://localhost:7071/api/")]
        public string WebApiUrl { get; set; } = "http://localhost:7071/api/";

        [Option('k', "api-key", HelpText = "API Key", Required = false)]
        public string? ApiKey { get; set; }

        [Option('g', "games", HelpText = "Number of simultaneous games", Default = 1)]
        public int Games { get; set; } = 1;
    }
}
