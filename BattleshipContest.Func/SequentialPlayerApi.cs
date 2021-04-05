using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using NBattleshipCodingContest.Logic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace BattleshipContestFunc
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

        [Function("GetShotSequentialPlayer")]
        public async Task<HttpResponseData> GetShot(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "players/sequential/getShot")] HttpRequestData req)
        {
            using var reader = new StreamReader(req.Body);
            var bodyContent = await reader.ReadToEndAsync();
            ShotRequest request = JsonSerializer.Deserialize<ShotRequest>(bodyContent, jsonOptions)!;

            var response = req.CreateResponse();
            await response.WriteAsJsonAsync(request.LastShot?.Next() ?? new BoardIndex("A1"), jsonSerializer);
            return response;
        }
    }
}
