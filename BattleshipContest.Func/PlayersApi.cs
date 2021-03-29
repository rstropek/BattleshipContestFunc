using System;
using System.Collections.Generic;
using System.IO;
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
    public record PlayerAddDto(Guid Id, string Name, string WebApiUrl, string? ApiKey = null);

    public class PlayersApi
    {
        private readonly IPlayerTable playerTable;
        private readonly IMapper mapper;
        private readonly JsonSerializerOptions jsonOptions;
        private readonly JsonObjectSerializer jsonSerializer;
        private readonly IAuthorize authorize;

        public PlayersApi(IPlayerTable playerTable, IMapper mapper, JsonSerializerOptions jsonOptions,
            JsonObjectSerializer jsonSerializer, IAuthorize authorize)
        {
            this.playerTable = playerTable;
            this.mapper = mapper;
            this.jsonOptions = jsonOptions;
            this.jsonSerializer = jsonSerializer;
            this.authorize = authorize;
        }

        [Function("Get")]
        public async Task<HttpResponseData> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "players")] HttpRequestData req)
        {
            // Verify authenticated user is present
            var subject = await authorize.TryGetSubject(req.Headers);
            if (subject == null)
            {
                return req.CreateResponse(HttpStatusCode.Unauthorized);
            }

            var players = mapper.Map<List<Player>, List<PlayerGetDto>>(
                await playerTable.Get(p => p.Creator == subject));

            var response = req.CreateResponse();
            await response.WriteAsJsonAsync(players, jsonSerializer);
            return response;
        }

        [Function("GetSingle")]
        public async Task<HttpResponseData> GetSingle(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "players/{idString}")] HttpRequestData req,
            string idString)
        {
            // Verify authenticated user is present
            var subject = await authorize.TryGetSubject(req.Headers);
            if (subject == null)
            {
                return req.CreateResponse(HttpStatusCode.Unauthorized);
            }

            if (!Guid.TryParseExact(idString, "D", out var id))
            {
                return await req.CreateValidationErrorResponse(
                    "Could not parse specified ID, must be a GUID with format xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
                    jsonSerializer);
            }

            var player = await playerTable.GetSingle(id);
            if (player == null)
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            if (player.Creator != subject)
            {
                return req.CreateResponse(HttpStatusCode.Forbidden);
            }

            var response = req.CreateResponse();
            await response.WriteAsJsonAsync(mapper.Map<Player, PlayerGetDto>(player), jsonSerializer);
            return response;
        }

        [Function("Add")]
        public async Task<HttpResponseData> Add(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "players")] HttpRequestData req)
        {
            // Verify authenticated user is present
            var subject = await authorize.TryGetSubject(req.Headers);
            if (subject == null)
            {
                return req.CreateResponse(HttpStatusCode.Unauthorized);
            }

            // Deserialize and verify DTO
            using var reader = new StreamReader(req.Body);
            PlayerAddDto? player;
            try
            {
                player = JsonSerializer.Deserialize<PlayerAddDto>(await reader.ReadToEndAsync(), jsonOptions);
            }
            catch (JsonException ex)
            {
                return await req.CreateValidationErrorResponse($"Could not parse request body ({ex.Message})", jsonSerializer);
            }

            if (player == null)
            {
                return await req.CreateValidationErrorResponse($"Missing player in request body.", jsonSerializer);
            }

            if (string.IsNullOrWhiteSpace(player.Name))
            {
                return await req.CreateValidationErrorResponse($"Name must not be empty.", jsonSerializer);
            }

            if (string.IsNullOrWhiteSpace(player.WebApiUrl))
            {
                return await req.CreateValidationErrorResponse($"Web API URL must not be empty.", jsonSerializer);
            }

            if (!Uri.TryCreate(player.WebApiUrl, UriKind.Absolute, out var uri))
            {
                return await req.CreateValidationErrorResponse($"Web API URL must be a valid absolute URL.", jsonSerializer);
            }
            else
            {
                player = player with { WebApiUrl = Uri.EscapeUriString(uri.ToString()) };
            }

            // Set ID to new ID if empty
            if (player.Id == Guid.Empty)
            {
                player = player with { Id = Guid.NewGuid() };
            }

            // Create data object from DTO
            var playerToAdd = mapper.Map<PlayerAddDto, Player>(player);
            playerToAdd.Creator = subject;

            // Store player
            await playerTable.Add(playerToAdd);

            // Convert added player into DTO
            var playerToReturn = mapper.Map<Player, PlayerGetDto>(playerToAdd);

            var response = req.CreateResponse();
            await response.WriteAsJsonAsync(playerToReturn, jsonSerializer);
            response.StatusCode = HttpStatusCode.Created;
            return response;
        }

        [Function("Delete")]
        public async Task<HttpResponseData> Delete(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "players/{idString}")] HttpRequestData req,
            string idString)
        {
            // Verify authenticated user is present
            var subject = await authorize.TryGetSubject(req.Headers);
            if (subject == null)
            {
                return req.CreateResponse(HttpStatusCode.Unauthorized);
            }

            if (!Guid.TryParseExact(idString, "D", out var id))
            {
                return await req.CreateValidationErrorResponse(
                    "Could not parse specified ID, must be a GUID with format xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
                    jsonSerializer);
            }

            var entity = await playerTable.GetSingle(id);
            if (entity == null)
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            if (entity.Creator != subject)
            {
                return req.CreateResponse(HttpStatusCode.Forbidden);
            }

            await playerTable.Delete(entity);
            return req.CreateResponse(HttpStatusCode.NoContent);
        }
    }
}
