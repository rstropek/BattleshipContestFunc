using CommandLine;
using Microsoft.Extensions.Configuration;
using NBattleshipCodingContest.Logic;
using Serilog;
using System;
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

        private static GameConfiguration GetGameConfiguration()
        {
            var playerClientFactory = new PlayerHttpClientFactory();
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(Environment.CurrentDirectory, "appsettings.json"), true, false)
                .AddEnvironmentVariables()
                .Build();
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            var gameFactory = new SinglePlayerGameFactory(new RandomBoardFiller());

            return new GameConfiguration(playerClientFactory, gameFactory, configuration, jsonOptions, logger);
        }

        private static int TestPlayer(TestPlayerOptions options)
        {
            var (playerClientFactory, _, configuration, jsonOptions, logger) = GetGameConfiguration();

            var playerClient = new PlayerClient(playerClientFactory, configuration, jsonOptions);
            try
            {
                logger.Information("Sending *getReady* to player");
                playerClient.GetReady(options.WebApiUrl, options.ApiKey).Wait();
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
            var (playerClientFactory, gameFactory, configuration, jsonOptions, logger) = GetGameConfiguration();

            var playerClient = new PlayerClient(playerClientFactory, configuration, jsonOptions);
            var gameClient = new GameClient(playerClient, gameFactory);
            try
            {
                logger.Information("Starting tournament");
                var games = gameClient.CreateTournamentGames(options.Games);
                gameClient.PlaySimultaneousGames(options.WebApiUrl, games, 101, apiKey: options.ApiKey).Wait();
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
