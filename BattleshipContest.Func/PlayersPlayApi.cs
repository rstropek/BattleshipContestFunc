using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.Core.Serialization;
using BattleshipContestFunc.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BattleshipContestFunc
{
    public class PlayersPlayApi : PlayersApiBase
    {
        private readonly IAuthorize authorize;
        private readonly IGameClient gameClient;
        private readonly IPlayerLogTable playerLogTable;
        private readonly IPlayerResultTable playerResultTable;
        private readonly IPlayerGameLeaseManager playerGameLease;
        private readonly IMessageSender messageSender;
        private static string? serviceBusConnectionString;

        public PlayersPlayApi(IPlayerTable playerTable, JsonSerializerOptions jsonOptions,
            JsonObjectSerializer jsonSerializer, IAuthorize authorize, IGameClient gameClient,
            IPlayerLogTable playerLogTable, IPlayerResultTable playerResultTable, 
            IPlayerGameLeaseManager playerGameLease, IConfiguration configuration,
            IMessageSender messageSender)
            : base(playerTable, jsonOptions, jsonSerializer)
        {
            this.authorize = authorize;
            this.gameClient = gameClient;
            this.playerLogTable = playerLogTable;
            this.playerResultTable = playerResultTable;
            this.playerGameLease = playerGameLease;
            this.messageSender = messageSender;

            if (serviceBusConnectionString == null)
            {
                serviceBusConnectionString = configuration["AzureWebJobsServiceBus"];
            }
        }

        [Function("TestPlayer")]
        public async Task<HttpResponseData> Test(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "players/{idString}/test")] HttpRequestData req,
            string idString)
        {
            // Verify authenticated user is present
            var subject = await authorize.TryGetSubject(req.Headers);
            if (subject == null) return req.CreateResponse(HttpStatusCode.Unauthorized);

            var (player, errorResponse) = await GetSingleOwning(req, idString, subject);
            if (player == null) return errorResponse!;

            try
            {
                await gameClient.GetReadyForGame(player.WebApiUrl, player.ApiKey);
                await gameClient.PlaySingleMoveInRandomGame(player.WebApiUrl, player.ApiKey);
            }
            catch (Exception ex)
            {
                var errorMessage = ex.GetFullDescription();
                await playerLogTable.AddException(player.RowKey, player.WebApiUrl, errorMessage);
                return await CreateDependencyError(req, errorMessage);
            }

            await playerLogTable.Add(new(player.RowKey, player.WebApiUrl, $"Successfully tested player"));
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

        internal async Task<MeasurePlayerRequestMessage> RenewLease(MeasurePlayerRequestMessage message, bool force = false)
        {
            if (force || DateTime.UtcNow > message.LeaseEnd)
            {
                await playerGameLease.Renew(message.PlayerId, message.LeaseId);
                return message with { LeaseEnd = GetLeaseEnd() };
            }

            return message;
        }

        private const string TopicName = "MeasurePlayerTopic";

        [Function("PlayGame")]
        public async Task<HttpResponseData> Play(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "players/{idString}/play")] HttpRequestData req,
            string idString)
        {
            // Verify authenticated user is present
            var subject = await authorize.TryGetSubject(req.Headers);
            if (subject == null) return req.CreateResponse(HttpStatusCode.Unauthorized);

            var (player, errorResponse) = await GetSingleOwning(req, idString, subject);
            if (player == null) return errorResponse!;
            var playerId = player.GetPlayerIdGuid();

            try
            {
                await playerLogTable.Add(new(playerId, player.WebApiUrl, $"Getting player ready for tournament"));
                await gameClient.GetReadyForGame(player.WebApiUrl, player.ApiKey);
            }
            catch (Exception ex)
            {
                var errorMessage = ex.GetFullDescription();
                await playerLogTable.AddException(playerId, player.WebApiUrl, errorMessage);
                return await CreateDependencyError(req, $"Player not ready\n{errorMessage}");
            }

            string leaseId;
            try
            {
                leaseId = await playerGameLease.Acquire(playerId, LeaseDuration);
            }
            catch (RequestFailedException)
            {
                return await CreateConflictError(req, "Game already in progress"); 
            }

            var startedEntry = await playerLogTable.Add(
                new(playerId, player.WebApiUrl, $"Tournament") { Started = DateTime.UtcNow });
            player.TournamentInProgressSince = DateTime.UtcNow;
            await playerTable.Replace(player);

            var message = new MeasurePlayerRequestMessage(playerId, leaseId, GetLeaseEnd(),
                player.WebApiUrl, player.ApiKey, player.Name, startedEntry!.RowKey);
            await messageSender.SendMessage(message, serviceBusConnectionString!, 
                TopicName, TimeSpan.FromMinutes(1));

            return req.CreateResponse(HttpStatusCode.Accepted);
        }

        internal const int NumberOfGames = 25;
        internal const int ParallelGames = 25;

        [Function("AsyncPlayGame")]
        public async Task AsyncGame(
            [ServiceBusTrigger(TopicName, "MeasurePlayerSubscription")] string sbMessage,
            FunctionContext context)
        {
            var logger = context.GetLogger<PlayersPlayApi>();
            if (string.IsNullOrEmpty(sbMessage))
            {
                logger.LogCritical("Received empty message");
                return;
            }

            MeasurePlayerRequestMessage? message;
            try
            {
                message = JsonSerializer.Deserialize<MeasurePlayerRequestMessage>(sbMessage, jsonOptions);
            }
            catch (JsonException ex)
            {
                logger.LogCritical($"Cloud not parse message {sbMessage}: {ex.Message}");
                return;
            }

            if (message == null)
            {
                logger.LogCritical($"Message {sbMessage} empty after deserialization");
                return;
            }

            var validationError = ValidateModel(message);
            if (validationError != null)
            {
                logger.LogCritical($"Message {sbMessage} invalid");
                return;
            }

            message = await RenewLease(message);

            var numberOfErrors = 0;
            IEnumerable<int> numberOfShots;
            const int maxNumberOfErrors = 3;
            var gameLogEntry = await playerLogTable.Add(
                new(message.PlayerId, message.WebApiUrl, $"Games {message.CompletedGameCount + 1}-{message.CompletedGameCount + ParallelGames}") { Started = DateTime.UtcNow });
            while (true)
            {
                try
                {
                    numberOfShots = await gameClient.PlaySimultaneousGames(
                        message.WebApiUrl,
                        ParallelGames,
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
                        return;
                    }
                }
            }

            gameLogEntry!.Completed = DateTime.UtcNow;
            gameLogEntry!.LogMessage = $"Games {message.CompletedGameCount + 1}-{message.CompletedGameCount + ParallelGames} ({numberOfShots.Sum()} shots)";
            await playerLogTable.Replace(gameLogEntry);

            message = message with
            {
                CompletedGameCount = message.CompletedGameCount + ParallelGames,
                NumberOfShots = message.NumberOfShots + numberOfShots.Sum()
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
                return;
            }

            message = await RenewLease(message, true);
            await messageSender.SendMessage(message, serviceBusConnectionString!,
                TopicName, TimeSpan.FromMinutes(1));
        }
    }
}
