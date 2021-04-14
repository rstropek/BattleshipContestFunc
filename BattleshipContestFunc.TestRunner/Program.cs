using CommandLine;
using Microsoft.Extensions.Configuration;
using NBattleshipCodingContest.Logic;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace BattleshipContestFunc.TestRunner
{
    internal record GameConfiguration(
        PlayerHttpClientFactory PlayerFactory,
        SinglePlayerGameFactory GameFactory,
        IConfiguration Configuration,
        JsonSerializerOptions JsonOptions,
        ILogger Logger);

    class Program
    {
        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<TestPlayerOptions, RunTournamentOptions>(args)
                .MapResult(
                  (TestPlayerOptions options) => TestPlayer(options),
                  (RunTournamentOptions options) => RunTournament(options),
                  errors => 1);
        }

        private static GameConfiguration GetGameConfiguration(IEnumerable<KeyValuePair<string, string>> settings)
        {
            var playerClientFactory = new PlayerHttpClientFactory();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .AddJsonFile(Path.Combine(Environment.CurrentDirectory, "appsettings.json"), true, false)
                .AddEnvironmentVariables()
                .Build();
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            var logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .CreateLogger();

            var gameFactory = new SinglePlayerGameFactory(new RandomBoardFiller());

            return new GameConfiguration(playerClientFactory, gameFactory, configuration, jsonOptions, logger);
        }

        private static int TestPlayer(TestPlayerOptions options)
        {
            var (playerClientFactory, _, configuration, jsonOptions, logger) = 
                GetGameConfiguration(new KeyValuePair<string, string>[]
                {
                    new("Timeouts:getReady", options.Timeout.ToString()),
                    new("Timeouts:getShot", "3000"),
                    new("Timeouts:getShots", "3000"),
                    new("Timeouts:finished", "3000"),
                });

            var playerClient = new PlayerClient(playerClientFactory, configuration, jsonOptions);
            try
            {
                logger.Information("Sending *getReady* to player");
                playerClient.GetReady(options.WebApiUrl, 1, options.ApiKey).Wait();
                logger.Information("Successfully sent *getReady* to player");
                return 0;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while sending *getReady* to player");
            }

            return -1;
        }

        private static int RunTournament(RunTournamentOptions options)
        {
            var (playerClientFactory, gameFactory, configuration, jsonOptions, logger) = 
                GetGameConfiguration(new KeyValuePair<string, string>[]
                {
                    new("Timeouts:getReady", options.GetReadyTimeout.ToString()),
                    new("Timeouts:getShot", options.GetShotsTimeout.ToString()),
                    new("Timeouts:getShots", options.GetShotsTimeout.ToString()),
                    new("Timeouts:finished", "3000"),
                });

            var playerClient = new PlayerClient(playerClientFactory, configuration, jsonOptions);
            var gameClient = new GameClient(playerClient, gameFactory);
            try
            {
                logger.Information("Starting tournament");
                var games = gameClient.CreateTournamentGames(options.Games);
                gameClient.PlaySimultaneousGames(options.WebApiUrl, games, 101, apiKey: options.ApiKey).Wait();

                try
                {
                    gameClient.NotifyGameFinished(options.WebApiUrl, games, options.ApiKey).Wait();
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Could not send 'Finished' message to player. Player probably does not implement it. That's is fine, it is optional.\n\n{ex.GetFullDescription()}";
                    logger.Warning(errorMessage);
                }

                var stats = games.Analyze();
                logger.Information("Successfully completed tournament. Avg. shots: {Avg}, std. dev.: {StdDev}",
                    stats.Average, stats.StdDev);
                return 0;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error during tournament");
            }

            return -1;
        }
    }
}
