using CommandLine;

namespace BattleshipContestFunc.TestRunner
{

    [Verb("test", HelpText = "Tests a player by sending a *getReady* message and one *getShot*.")]
    internal class TestPlayerOptions
    {
        [Option('a', "web-api-url", HelpText = "Web API URL", Default = "http://localhost:7071/api/")]
        public string WebApiUrl { get; set; }
    }
}
