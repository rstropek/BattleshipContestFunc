using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
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
                await gameClient.GetReadyForGame(entity.WebApiUrl, entity.ApiKey);
                await gameClient.PlaySingleMoveInRandomGame(entity.WebApiUrl, entity.ApiKey);
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

        public record MeasurePlayerRequestMessage(
            Guid PlayerId,
            [property: Required][property: MinLength(1)] string LeaseId,
            DateTime LeaseEnd,
            [property: Required][property: AbsoluteUri][property: MinLength(1)] string WebApiUrl,
            string? ApiKey,
            [property: Required][property: MinLength(1)] string PlayerName,
            [property: Required][property: MinLength(1)] string TournamentStartedLogRowKey,
            int CompletedGameCount = 0,
            int NumberOfShots = 0);

        private static readonly TimeSpan LeaseBuffer = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(60);

        private static DateTime GetLeaseEnd() => DateTime.UtcNow + LeaseDuration - LeaseBuffer;

        private async Task<MeasurePlayerRequestMessage> RenewLease(MeasurePlayerRequestMessage message, bool force = false)
        {
            if (force || DateTime.UtcNow > message.LeaseEnd)
            {
                await playerGameLease.Renew(message.PlayerId, message.LeaseId);
                return message with { LeaseEnd = GetLeaseEnd() };
            }

            return message;
        }

        [Function("PlayGame")]
        public async Task<PlayGameOutput> Play(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "players/{idString}/play")] HttpRequestData req,
            string idString)
        {
            // Verify authenticated user is present
            var subject = await authorize.TryGetSubject(req.Headers);
            if (subject == null) return new PlayGameOutput() { HttpResponse = req.CreateResponse(HttpStatusCode.Unauthorized) }; ;

            var (entity, errorResponse) = await GetSingleOwning(req, idString, subject);
            if (entity == null) return new PlayGameOutput() { HttpResponse = errorResponse };
            var playerId = Guid.Parse(entity.RowKey);

            try
            {
                await playerLogTable.Add(new(playerId, entity.WebApiUrl, $"Getting player ready for tournament"));
                await gameClient.GetReadyForGame(entity.WebApiUrl, entity.ApiKey);
            }
            catch (Exception ex)
            {
                var errorMessage = ex.GetFullDescription();
                await playerLogTable.AddException(playerId, entity.WebApiUrl, errorMessage);
                return new PlayGameOutput
                {
                    Message = null,
                    HttpResponse = await CreateDependencyError(req, $"Player not ready\n{errorMessage}")
                };
            }

            string leaseId;
            try
            {
                leaseId = await playerGameLease.Acquire(playerId, LeaseDuration);
            }
            catch (RequestFailedException)
            {
                return new PlayGameOutput
                {
                    Message = null,
                    HttpResponse = await CreateConflictError(req, "Game already in progress")
                };
            }

            var startedEntry = await playerLogTable.Add(
                new(playerId, entity.WebApiUrl, $"Tournament") { Started = DateTime.UtcNow });
            entity.TournamentInProgressSince = DateTime.UtcNow;
            await playerTable.Replace(entity);

            var message = new MeasurePlayerRequestMessage(playerId, leaseId, GetLeaseEnd(),
                entity.WebApiUrl, entity.ApiKey, entity.Name, startedEntry!.RowKey);
            return new PlayGameOutput
            {
                Message = JsonSerializer.Serialize(message, jsonOptions),
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

            var validationError = ValidateModel(message);
            if (validationError != null) return null;

            message = await RenewLease(message);

            var numberOfErrors = 0;
            var numberOfShots = 0;
            const int maxNumberOfErrors = 3;
            var gameLogEntry = await playerLogTable.Add(
                new(message.PlayerId, message.WebApiUrl, $"Game {message.CompletedGameCount + 1}") { Started = DateTime.UtcNow });
            while (true)
            {
                try
                {
                    numberOfShots = await gameClient.PlayGame(
                        message.WebApiUrl,
                        async () => { message = await RenewLease(message); },
                        message.ApiKey);
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
                        await playerGameLease.Release(message.PlayerId, message.LeaseId);
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

                await playerGameLease.Release(message.PlayerId, message.LeaseId);
                return null;
            }

            message = await RenewLease(message, true);
            return JsonSerializer.Serialize(message, jsonOptions);
        }
    }
}
