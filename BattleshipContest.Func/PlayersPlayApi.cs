using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using BattleshipContestFunc.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace BattleshipContestFunc
{
    public partial class PlayersApi : ApiBase
    {
        [Function("TestPlayer")]
        public async Task<HttpResponseData> Test(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "players/{idString}/test")] HttpRequestData req,
            string idString)
        {
            // Verify authenticated user is present
            var subject = await authorize.TryGetSubject(req.Headers);
            if (subject == null) return req.CreateResponse(HttpStatusCode.Unauthorized);

            var (entity, errorResponse) = await GetSingleOwning(req, idString, subject);
            if (entity == null) return errorResponse!;

            try
            {
                await playerClient.GetReady(entity.WebApiUrl, entity.ApiKey);
                await playerClient.PlaySingleMoveInRandomGame(entity.WebApiUrl, entity.ApiKey);
            }
            catch (Exception ex)
            {
                var errorMessage = ex.GetFullDescription();
                await playerLogTable.AddException(entity.RowKey, entity.WebApiUrl, errorMessage);
                return await CreateDependencyError(req, errorMessage);
            }

            await playerLogTable.Add(new(entity.RowKey, entity.WebApiUrl, $"Successfully tested player"));
            return req.CreateResponse(HttpStatusCode.OK);
        }

        public record MeasurePlayerRequestMessage(Guid PlayerId, int CompletedGameCount = 0, int NumberOfShots = 0,
            string? WebApiUrl = null, string? ApiKey = null, string? PlayerName = null,
            string? TournamentStartedLogRowKey = null);

        [Function("PlayGame")]
        public async Task<PlayGameOutput> Game(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "players/{idString}/play")] HttpRequestData req,
            string idString)
        {
            // Verify authenticated user is present
            var subject = await authorize.TryGetSubject(req.Headers);
            if (subject == null) return new PlayGameOutput() { HttpResponse = req.CreateResponse(HttpStatusCode.Unauthorized) }; ;

            var (entity, errorResponse) = await GetSingleOwning(req, idString, subject);
            if (entity == null) return new PlayGameOutput() { HttpResponse = errorResponse };

            return new PlayGameOutput
            {
                Message = JsonSerializer.Serialize(new MeasurePlayerRequestMessage(Guid.Parse(entity.RowKey)), jsonOptions),
                HttpResponse = req.CreateResponse(HttpStatusCode.Accepted)
            };
        }

        public class PlayGameOutput
        {
            [ServiceBusOutput("MeasurePlayerTopic", EntityType.Topic)]
            public string? Message { get; set; } = string.Empty;

            public HttpResponseData? HttpResponse { get; set; }
        }

        private const int NumberOfGames = 25;

        [Function("AsyncPlayGame")]
        [ServiceBusOutput("MeasurePlayerTopic", EntityType.Topic)]
        public async Task<string?> AsyncGame(
            [ServiceBusTrigger("MeasurePlayerTopic", "MeasurePlayerSubscription")] string sbMessage,
            FunctionContext context)
        {
            var logger = context.GetLogger("AsyncGame");
            if (string.IsNullOrEmpty(sbMessage))
            {
                logger.LogCritical("Received empty message");
                return null;
            }

            MeasurePlayerRequestMessage? message;
            try
            {
                message = JsonSerializer.Deserialize<MeasurePlayerRequestMessage>(sbMessage, jsonOptions);
            }
            catch (JsonException ex)
            {
                logger.LogCritical($"Cloud not parse message {sbMessage}: {ex.Message}");
                return null;
            }

            if (message == null)
            {
                logger.LogCritical($"Message {sbMessage} empty after deserialization");
                return null;
            }

            if (message.CompletedGameCount > 0)
            {
                if (string.IsNullOrEmpty(message.WebApiUrl))
                {
                    logger.LogCritical($"Web API URL must not be empty");
                    return null;
                }

                if (string.IsNullOrEmpty(message.PlayerName))
                {
                    logger.LogCritical($"Player name must not be empty");
                    return null;
                }

                if (string.IsNullOrEmpty(message.TournamentStartedLogRowKey))
                {
                    logger.LogCritical($"Row key of log entry for tournament start must not be empty");
                    return null;
                }
            }
            else
            {
                var player = await playerTable.GetSingle(message.PlayerId);
                if (player == null)
                {
                    logger.LogCritical($"Player {message.PlayerId} not found");
                    return null;
                }

                if (string.IsNullOrEmpty(player.WebApiUrl))
                {
                    logger.LogCritical($"Message {sbMessage} does not contain a web api url");
                    return null;
                }

                if (string.IsNullOrEmpty(player.Name))
                {
                    logger.LogCritical($"Message {sbMessage} does not contain a player name");
                    return null;
                }

                try
                {
                    await playerLogTable.Add(new(message.PlayerId, player.WebApiUrl, $"Getting player ready for tournament"));
                    await playerClient.GetReady(player.WebApiUrl, player.ApiKey);
                }
                catch (Exception ex)
                {
                    var errorMessage = ex.GetFullDescription();
                    await playerLogTable.AddException(message.PlayerId, player.WebApiUrl, errorMessage);
                    return null;
                }

                var startedEntry = await playerLogTable.Add(
                    new(message.PlayerId, player.WebApiUrl, $"Tournament") { Started = DateTime.UtcNow });
                player.TournamentInProgressSince = DateTime.UtcNow;
                await playerTable.Replace(player);

                message = message with
                {
                    WebApiUrl = player.WebApiUrl,
                    ApiKey = player.ApiKey,
                    PlayerName = player.Name,
                    TournamentStartedLogRowKey = startedEntry!.RowKey
                };
            }

            var numberOfErrors = 0;
            var numberOfShots = 0;
            const int maxNumberOfErrors = 3;
            var gameLogEntry = await playerLogTable.Add(
                new(message.PlayerId, message.WebApiUrl, $"Game {message.CompletedGameCount + 1}") { Started = DateTime.UtcNow });
            while (true)
            { 

                try
                {
                    numberOfShots = await playerClient.PlayGame(message.WebApiUrl, message.ApiKey);
                    break;
                }
                catch (Exception ex)
                {
                    var errorMessage = ex.GetFullDescription();
                    await playerLogTable.AddException(message.PlayerId, message.WebApiUrl, errorMessage);

                    numberOfErrors++;
                    if (numberOfErrors > maxNumberOfErrors)
                    {
                        await playerLogTable.Add(new(message.PlayerId, "Too many errors, stopping tournament"));
                        return null;
                    }
                }
            }

            gameLogEntry!.Completed = DateTime.UtcNow;
            await playerLogTable.Replace(gameLogEntry);

            message = message with
            {
                CompletedGameCount = message.CompletedGameCount + 1,
                NumberOfShots = message.NumberOfShots + numberOfShots
            };

            if (message.CompletedGameCount == NumberOfGames)
            {
                var avgShots = ((double)message.NumberOfShots) / NumberOfGames;
                var playerResultEntry = await playerResultTable.GetSingle(message.PlayerId);
                if (playerResultEntry == null)
                {
                    playerResultEntry = new(message.PlayerId)
                    {
                        Name = message.PlayerName
                    };
                }

                playerResultEntry.LastMeasurement = DateTime.UtcNow;
                playerResultEntry.AvgNumberOfShots = avgShots;
                await playerResultTable.Replace(playerResultEntry);

                var logEntry = await playerLogTable.GetSingle(message.PlayerId, message.TournamentStartedLogRowKey);
                if (logEntry != null)
                {
                    logEntry.LogMessage = $"Finished tournament with total # of shots {message.NumberOfShots}, avg # of shots {avgShots}";
                    logEntry.Completed = DateTime.UtcNow;
                    await playerLogTable.Replace(logEntry);
                }

                var player = await playerTable.GetSingle(message.PlayerId);
                if (player != null)
                {
                    player.TournamentInProgressSince = null;
                    await playerTable.Replace(player);
                }

                return null;
            }

            return JsonSerializer.Serialize(message, jsonOptions);
        }
    }
}
