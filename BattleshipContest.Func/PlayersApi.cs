using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    public record PlayerGetDto(Guid Id, string Name, string WebApiUrl, string Creator);
    public record PlayerPatchDto(
        string? Name = null,
        [property: AbsoluteUri] string? WebApiUrl = null,
        string? ApiKey = null);
    public record PlayerAddDto(
        Guid Id,
        [property: Required][property: MinLength(1)] string Name,
        [property: Required][property: AbsoluteUri][property: MinLength(1)] string WebApiUrl,
        string? ApiKey = null);

    public class PlayersApi : ApiBase
    {
        private readonly IPlayerTable playerTable;
        private readonly IMapper mapper;
        private readonly IAuthorize authorize;

        public PlayersApi(IPlayerTable playerTable, IMapper mapper, JsonSerializerOptions jsonOptions,
            JsonObjectSerializer jsonSerializer, IAuthorize authorize)
            : base(jsonOptions, jsonSerializer)
        {
            this.playerTable = playerTable;
            this.mapper = mapper;
            this.authorize = authorize;
        }

        private async Task<(Player?, HttpResponseData?)> GetSingleOwning(HttpRequestData req, Guid id, string ownerSubject)
        {
            var entity = await playerTable.GetSingle(id);
            if (entity == null) return (null, req.CreateResponse(HttpStatusCode.NotFound));
            if (entity.Creator != ownerSubject) return (null, req.CreateResponse(HttpStatusCode.Forbidden));
            return (entity, null);
        }

        [Function("Get")]
        public async Task<HttpResponseData> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "players")] HttpRequestData req)
        {
            // Verify authenticated user is present
            var subject = await authorize.TryGetSubject(req.Headers);
            if (subject == null) return req .CreateResponse(HttpStatusCode.Unauthorized);

            // Get and return all players of current user 
            var players = mapper.Map<List<Player>, List<PlayerGetDto>>(await playerTable.Get(p => p.Creator == subject));
            return await CreateResponse(req, players);
        }

        [Function("GetSingle")]
        public async Task<HttpResponseData> GetSingle(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "players/{idString}")] HttpRequestData req,
            string idString)
        {
            // Verify authenticated user is present
            var subject = await authorize.TryGetSubject(req.Headers);
            if (subject == null) return req.CreateResponse(HttpStatusCode.Unauthorized);

            if (!Guid.TryParseExact(idString, "D", out var id)) return await CreateValidationError(req, GuidParseErrorMessage);

            var (player, errorResponse) = await GetSingleOwning(req, id, subject);
            if (player == null) return errorResponse!;

            return await CreateResponse(req, mapper.Map<Player, PlayerGetDto>(player));
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

            if (!Guid.TryParseExact(idString, "D", out var id)) return await CreateValidationError(req, GuidParseErrorMessage);

            var (entity, owningErrorResponse) = await GetSingleOwning(req, id, subject);
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

            if (player.ApiKey != null && player.ApiKey != entity.ApiKey)
            {
                entity.ApiKey = player.ApiKey.Length == 0 ? null : player.ApiKey;
                update = true;
            }

            if (update) await playerTable.Replace(entity);

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

            if (!Guid.TryParseExact(idString, "D", out var id)) return await CreateValidationError(req, GuidParseErrorMessage);

            var (entity, errorResponse) = await GetSingleOwning(req, id, subject);
            if (entity == null) return errorResponse!;

            await playerTable.Delete(entity);
            return req.CreateResponse(HttpStatusCode.NoContent);
        }
    }
}
