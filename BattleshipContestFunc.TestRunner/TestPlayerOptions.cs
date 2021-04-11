﻿using CommandLine;

namespace BattleshipContestFunc.TestRunner
{

    [Verb("test", HelpText = "Tests a player by sending a *getReady* request.")]
    internal class TestPlayerOptions
    {
        [Option('a', "api-url", HelpText = "Web API URL", Default = "http://localhost:7071/api/")]
        public string WebApiUrl { get; set; } = "http://localhost:7071/api/";

        [Option('k', "api-key", HelpText = "API Key", Required = false)]
        public string? ApiKey { get; set; }
    }
}
