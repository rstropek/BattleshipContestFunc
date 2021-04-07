using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using NBattleshipCodingContest.Logic;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace BattleshipContestFunc.Players
{
    public class RandomPlayerApi : ApiBase
    {
        public RandomPlayerApi(JsonSerializerOptions jsonOptions, JsonObjectSerializer jsonSerializer)
            : base(jsonOptions, jsonSerializer)
        {
        }

        [Function("GetReadyRandomPlayer")]
        [SuppressMessage("Performance", "CA1822", Justification = "Just a demo player")]
        public HttpResponseData GetReady(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "players/random/getReady")] HttpRequestData req)
        { 
            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function("GetShotsRandomPlayer")]
        public async Task<HttpResponseData> GetShots(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "players/random/getShots")] HttpRequestData req)
        {
            using var reader = new StreamReader(req.Body);
            ShotRequest[]? requests;
            try
            {
                var bodyContent = await reader.ReadToEndAsync();
                requests = JsonSerializer.Deserialize<ShotRequest[]>(bodyContent, jsonOptions);
            }
            catch (JsonException ex)
            {
                return await CreateValidationError(req, $"Could not parse request body ({ex.Message})");
            }

            if (requests == null)
            {
                return await CreateValidationError(req, $"Missing board content in request body.");
            }

            var rand = new Random();
            var response = req.CreateResponse();
            await response.WriteAsJsonAsync(requests.Select(_ => new BoardIndex(rand.Next(10), rand.Next(10))), jsonSerializer);
            return response;
        }
    }
}
