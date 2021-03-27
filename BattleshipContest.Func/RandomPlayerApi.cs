using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using NBattleshipCodingContest.Logic;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace BattleshipContestFunc
{
    public class RandomPlayerApi
    {
        private readonly JsonSerializerOptions jsonOptions;
        private readonly JsonObjectSerializer jsonSerializer;

        public RandomPlayerApi(JsonSerializerOptions jsonOptions, JsonObjectSerializer jsonSerializer)
        {
            this.jsonOptions = jsonOptions;
            this.jsonSerializer = jsonSerializer;
        }

        [Function("GetReadyRandomPlayer")]
        [SuppressMessage("Performance", "CA1822", Justification = "Just a demo player")]
        public HttpResponseData GetReady(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "players/random/getReady")] HttpRequestData req)
        { 
            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function("GetShotRandomPlayer")]
        public async Task<HttpResponseData> GetShot(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "players/random/getShot")] HttpRequestData req)
        {
            using var reader = new StreamReader(req.Body);
            BoardContent? boardContent;
            try
            {
                var bodyContent = await reader.ReadToEndAsync();
                boardContent = JsonSerializer.Deserialize<BoardContent>(bodyContent, jsonOptions);
            }
            catch (JsonException ex)
            {
                return await req.CreateValidationErrorResponse($"Could not parse request body ({ex.Message})", jsonSerializer);
            }

            if (boardContent == null)
            {
                return await req.CreateValidationErrorResponse($"Missing board content in request body.", jsonSerializer);
            }

            var rand = new Random();
            var response = req.CreateResponse();
            await response.WriteAsJsonAsync(new BoardIndex(rand.Next(10), rand.Next(10)), jsonSerializer);
            return response;
        }
    }
}
