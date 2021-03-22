using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using Azure.Core.Serialization;
using BattleshipContestFunc.Data;
using Microsoft.Azure.Cosmos.Table.Queryable;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace BattleshipContestFunc
{
    public record PlayerDto(Guid Id, string Name, string WebApiUrl);

    public class PlayersApi
    {
        private readonly IPlayerTable playerTable;
        private readonly IMapper mapper;
        private readonly JsonSerializerOptions jsonOptions;
        private readonly JsonObjectSerializer jsonSerializer;

        public PlayersApi(IPlayerTable playerTable, IMapper mapper, JsonSerializerOptions jsonOptions,
            JsonObjectSerializer jsonSerializer)
        {
            this.playerTable = playerTable;
            this.mapper = mapper;
            this.jsonOptions = jsonOptions;
            this.jsonSerializer = jsonSerializer;
        }

        [Function("Get")]
        public async Task<HttpResponseData> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "players")] HttpRequestData req)
        {
            var players = mapper.Map<List<Player>, List<PlayerDto>>(await playerTable.Get());

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(players, jsonSerializer);
            return response;
        }

        [Function("GetSingle")]
        public async Task<HttpResponseData> GetSingle(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "players/{idString}")] HttpRequestData req,
            string idString)
        {
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

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(mapper.Map<Player, PlayerDto>(player), jsonSerializer);
            return response;
        }

        [Function("Add")]
        public async Task<HttpResponseData> Add(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "players")] HttpRequestData req)
        {
            using var reader = new StreamReader(req.Body);
            PlayerDto? player;
            try
            {
                player = JsonSerializer.Deserialize<PlayerDto>(await reader.ReadToEndAsync(), jsonOptions);
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

            if (player.Id == Guid.Empty)
            {
                player = player with { Id = Guid.NewGuid() };
            }

            await playerTable.Add(mapper.Map<PlayerDto, Player>(player));

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(player, jsonSerializer);
            return response;
        }

        [Function("Delete")]
        public async Task<HttpResponseData> Delete(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "players/{idString}")] HttpRequestData req,
            string idString)
        {
            if (!Guid.TryParseExact(idString, "D", out var id))
            {
                return await req.CreateValidationErrorResponse(
                    "Could not parse specified ID, must be a GUID with format xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
                    jsonSerializer);
            }

            await playerTable.Delete(id);
            return req.CreateResponse(HttpStatusCode.NoContent);
        }
    }
}
