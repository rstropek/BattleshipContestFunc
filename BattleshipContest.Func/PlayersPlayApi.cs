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

        public record MeasurePlayerRequestMessage(Guid PlayerId);

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

        [Function("AsyncPlayGame")]
        public async Task AsyncGame(
            [ServiceBusTrigger("MeasurePlayerTopic", "MeasurePlayerSubscription")] string sbMessage,
            FunctionContext context)
        {
            var logger = context.GetLogger("AsyncGame");
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

            var player = await playerTable.GetSingle(message.PlayerId);
            if (player == null)
            {
                logger.LogCritical($"Player {message.PlayerId} not found");
                return;
            }

            player.TournamentInProgressSince = DateTime.UtcNow;
            await playerTable.Replace(player);

            if (string.IsNullOrEmpty(player.WebApiUrl))
            {
                logger.LogCritical($"Message {sbMessage} does not contain a web api url");
                return;
            }

            if (string.IsNullOrEmpty(player.Name))
            {
                logger.LogCritical($"Message {sbMessage} does not contain a player name");
                return;
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
                return;
            }

            const int numberOfGames = 100;
            const int numberOfShotsForException = 200;
            var totalNumberOfShots = 0;
            var numberOfErrors = 0;
            const int maxNumberOfErrors = 5;
            await playerLogTable.Add(new(message.PlayerId, player.WebApiUrl, $"Starting tournament"));
            for (var i = 0; i < numberOfGames; i++)
            {
                try
                {
                    totalNumberOfShots += await playerClient.PlayGame(player.WebApiUrl, player.ApiKey);
                }
                catch (Exception ex)
                {
                    var errorMessage = ex.GetFullDescription();
                    await playerLogTable.AddException(message.PlayerId, player.WebApiUrl, errorMessage);

                    numberOfErrors++;
                    if (numberOfErrors > maxNumberOfErrors)
                    {
                        await playerLogTable.Add(new(message.PlayerId, "Too many errors, stopping tournament with max number of shots."));
                        return;
                    }

                    totalNumberOfShots += numberOfShotsForException;
                }
            }

            var avgShots = ((double)totalNumberOfShots) / numberOfGames;
            await playerResultTable.Add(new(message.PlayerId)
            {
                Name = player.Name,
                AvgNumberOfShots = avgShots,
                LastMeasurement = DateTime.UtcNow
            });
            await playerLogTable.Add(new(message.PlayerId, player.WebApiUrl, $"Finished tournament with total # of shots {totalNumberOfShots}, avg # of shots {avgShots}"));

            player = await playerTable.GetSingle(message.PlayerId);
            if (player != null)
            {
                player.TournamentInProgressSince = null;
                await playerTable.Replace(player);
            }
        }
    }
}
