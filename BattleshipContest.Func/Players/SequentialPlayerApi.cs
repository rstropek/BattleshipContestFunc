using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using NBattleshipCodingContest.Logic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace BattleshipContestFunc.Players
{
    public class SequentialPlayerApi : ApiBase
    {
        public SequentialPlayerApi(JsonSerializerOptions jsonOptions, JsonObjectSerializer jsonSerializer)
            : base(jsonOptions, jsonSerializer)
        {
        }

        [Function("GetReadySequentialPlayer")]
        [SuppressMessage("Performance", "CA1822", Justification = "Just a demo player")]
        public HttpResponseData GetReady(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "players/sequential/getReady")] HttpRequestData req)
        { 
            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function("GetShotsSequentialPlayer")]
        public async Task<HttpResponseData> GetShot(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "players/sequential/getShots")] HttpRequestData req)
        {
            using var reader = new StreamReader(req.Body);
            var bodyContent = await reader.ReadToEndAsync();
            var request = JsonSerializer.Deserialize<ShotRequest[]>(bodyContent, jsonOptions)!;

            var shots = new BoardIndex[request.Length];
            for(var i = 0; i < request.Length; i++)
            {
                shots[i] = request[i].LastShot?.Next() ?? new BoardIndex("A1");
            }

            var response = req.CreateResponse();
            await response.WriteAsJsonAsync(shots, jsonSerializer);
            return response;
        }
    }
}
