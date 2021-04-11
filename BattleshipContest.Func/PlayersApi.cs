using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using Azure.Core.Serialization;
using BattleshipContestFunc.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace BattleshipContestFunc
{
    public record PlayerGetDto(
        Guid Id, 
        string Name, 
        string WebApiUrl, 
        string Creator, 
        bool HasApiKey,
        bool NeedsThrottling,
        DateTime? LastMeasurement,
        double? AvgNumberOfShots,
        double? StdDev,
        string? GitHubUrl,
        DateTime? TournamentInProgressSince);
    public record PlayerPatchDto(
        string? Name = null,
        [property: AbsoluteUri] string? WebApiUrl = null,
        string? ApiKey = null,
        [property: AbsoluteUri] string? GitHubUrl = null,
        bool? NeedsThrottling = null);
    public record PlayerAddDto(
        Guid Id,
        [property: Required][property: MinLength(1)] string Name,
        [property: Required][property: AbsoluteUri][property: MinLength(1)] string WebApiUrl,
        string? ApiKey = null,
        [property: AbsoluteUri] string? GitHubUrl = null,
        bool? NeedsThrottling = null);
    public record PlayerLogDto(
        Guid PlayerId,
        DateTime Timestamp,
        Guid LogId,
        string LogMessage,
        string WebApiUrl,
        DateTime? Started,
        DateTime? Completed);

    public class PlayersApi : PlayersApiBase
    {
        private readonly IMapper mapper;
        private readonly IAuthorize authorize;
        private readonly IPlayerLogTable playerLogTable;
        private readonly IPlayerResultTable playerResultTable;

        public PlayersApi(IPlayerTable playerTable, IMapper mapper, JsonSerializerOptions jsonOptions,
            JsonObjectSerializer jsonSerializer, IAuthorize authorize,
            IPlayerLogTable playerLogTable, IPlayerResultTable playerResultTable)
            : base(playerTable, jsonOptions, jsonSerializer)
        {
            this.mapper = mapper;
            this.authorize = authorize;
            this.playerLogTable = playerLogTable;
            this.playerResultTable = playerResultTable;
        }

        [Function("Get")]
        public async Task<HttpResponseData> Get(
                [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "players")] HttpRequestData req)
        {
            // Verify authenticated user is present
            var subject = await authorize.TryGetSubject(req.Headers);
            if (subject == null) return req.CreateResponse(HttpStatusCode.Unauthorized);

            // Get and return all players of current user 
            var players = mapper.Map<List<Player>, List<PlayerGetDto>>(await playerTable.Get(p => p.Creator == subject));
            var results = await playerResultTable.Get();
            var resultingPlayers = players.Select(p =>
            {
                var result = results.FirstOrDefault(r => r.RowKey == p.Id.ToString());
                if (result != null)
                {
                    return p with
                    {
                        LastMeasurement = result.LastMeasurement,
                        AvgNumberOfShots = result.AvgNumberOfShots,
                        StdDev = result.StdDev
                    };
                }

                return p;
            }).ToList();

            return await CreateResponse(req, resultingPlayers);
        }

        [Function("GetSingle")]
        public async Task<HttpResponseData> GetSingle(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "players/{idString}")] HttpRequestData req,
            string idString)
        {
            // Verify authenticated user is present
            var subject = await authorize.TryGetSubject(req.Headers);
            if (subject == null) return req.CreateResponse(HttpStatusCode.Unauthorized);

            var (player, errorResponse) = await GetSingleOwning(req, idString, subject);
            if (player == null) return errorResponse!;

            return await CreateResponse(req, mapper.Map<Player, PlayerGetDto>(player));
        }

        [Function("GetPlayerLog")]
        public async Task<HttpResponseData> GetPlayerLog(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "players/{idString}/log")] HttpRequestData req,
            string idString)
        {
            // Verify authenticated user is present
            var subject = await authorize.TryGetSubject(req.Headers);
            if (subject == null) return req.CreateResponse(HttpStatusCode.Unauthorized);

            var (player, errorResponse) = await GetSingleOwning(req, idString, subject);
            if (player == null) return errorResponse!;

            var result = await playerLogTable.Get(player.GetPlayerIdGuid(), null);
            return await CreateResponse(req, mapper.Map<List<PlayerLog>, List<PlayerLogDto>>(
                result.OrderByDescending(l => l.Timestamp).ToList()));
        }

        [Function("ClearPlayerLog")]
        public async Task<HttpResponseData> ClearPlayerLog(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "players/{idString}/log/clear")] HttpRequestData req,
            string idString)
        {
            // Verify authenticated user is present
            var subject = await authorize.TryGetSubject(req.Headers);
            if (subject == null) return req.CreateResponse(HttpStatusCode.Unauthorized);

            var (player, errorResponse) = await GetSingleOwning(req, idString, subject);
            if (player == null) return errorResponse!;

            await playerLogTable.DeletePartition(player.GetPlayerIdGuid());
            await playerLogTable.Add(new(player.GetPlayerIdGuid(), "Log cleared"));

            return req.CreateResponse(HttpStatusCode.NoContent);
        }

        [Function("Add")]
        public async Task<HttpResponseData> Add(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "players")] HttpRequestData req)
        {
            // Verify authenticated user is present
            var subject = await authorize.TryGetSubject(req.Headers);
            if (subject == null) return req.CreateResponse(HttpStatusCode.Unauthorized);

            var (player, errorResponse) = await DeserializeAndValidateBody<PlayerAddDto>(req);
            if (player == null) return errorResponse!;

            var validationError = ValidateModel(player);
            if (validationError != null) return await CreateValidationError(req, validationError);

            // Set ID to new ID if empty
            if (player.Id == Guid.Empty) player = player with { Id = Guid.NewGuid() };

            // Create data object from DTO
            var playerToAdd = mapper.Map<PlayerAddDto, Player>(player);
            playerToAdd.Creator = subject;

            // Store player
            await playerTable.Add(playerToAdd);

            // Convert added player into DTO
            var playerToReturn = mapper.Map<Player, PlayerGetDto>(playerToAdd);

            await playerLogTable.Add(new(playerToAdd.RowKey, playerToAdd.WebApiUrl, "Created player"));
            return await CreateResponse(req, playerToReturn, HttpStatusCode.Created);
        }

        [Function("PatchPlayer")]
        public async Task<HttpResponseData> Patch(
            [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "players/{idString}")] HttpRequestData req,
            string idString)
        {
            // Verify authenticated user is present
            var subject = await authorize.TryGetSubject(req.Headers);
            if (subject == null) return req.CreateResponse(HttpStatusCode.Unauthorized);

            var (player, errorResponse) = await DeserializeAndValidateBody<PlayerPatchDto>(req);
            if (player == null) return errorResponse!;

            var validationError = ValidateModel(player);
            if (validationError != null) return await CreateValidationError(req, validationError);

            var (entity, owningErrorResponse) = await GetSingleOwning(req, idString, subject);
            if (entity == null) return owningErrorResponse!;

            var update = false;
            if (player.Name != null && player.Name != entity.Name)
            {
                if (player.Name.Length == 0) return await CreateValidationError(req, $"Name must not be empty.");
                entity.Name = player.Name;
                update = true;
            }

            if (player.WebApiUrl != null && player.WebApiUrl != entity.WebApiUrl)
            {
                entity.WebApiUrl = player.WebApiUrl;
                update = true;
            }

            if (player.NeedsThrottling != null && player.NeedsThrottling != entity.NeedsThrottling)
            {
                entity.NeedsThrottling = player.NeedsThrottling;
                update = true;
            }

            if (player.ApiKey != null && player.ApiKey != entity.ApiKey)
            {
                entity.ApiKey = player.ApiKey.Length == 0 ? null : player.ApiKey;
                update = true;
            }

            if (player.GitHubUrl != null && player.GitHubUrl != entity.GitHubUrl)
            {
                entity.GitHubUrl = player.GitHubUrl.Length == 0 ? null : player.GitHubUrl;
                update = true;
            }

            if (update) await playerTable.Replace(entity);



            await playerLogTable.Add(new(entity.RowKey, entity.WebApiUrl, $"Patched player"));
            return await CreateResponse(req, mapper.Map<Player, PlayerGetDto>(entity));
        }

        [Function("Delete")]
        public async Task<HttpResponseData> Delete(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "players/{idString}")] HttpRequestData req,
            string idString)
        {
            // Verify authenticated user is present
            var subject = await authorize.TryGetSubject(req.Headers);
            if (subject == null) return req.CreateResponse(HttpStatusCode.Unauthorized);

            var (entity, errorResponse) = await GetSingleOwning(req, idString, subject);
            if (entity == null) return errorResponse!;

            await playerTable.Delete(entity);

            await playerLogTable.Add(new(entity.RowKey, entity.WebApiUrl, $"Deleted player"));
            return req.CreateResponse(HttpStatusCode.NoContent);
        }
    }
}
