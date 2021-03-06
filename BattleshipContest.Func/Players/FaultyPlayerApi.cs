using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace BattleshipContestFunc.Players
{
    public class FaultyPlayerApi : ApiBase
    {
        public FaultyPlayerApi(JsonSerializerOptions jsonOptions, JsonObjectSerializer jsonSerializer)
            : base(jsonOptions, jsonSerializer)
        {
        }

        [Function("GetReadyFaultyPlayer")]
        [SuppressMessage("Performance", "CA1822", Justification = "Just a demo player")]
        public HttpResponseData GetReady(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "players/faulty/getReady")] HttpRequestData req)
        {
            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function("GetShotsFaultyPlayer")]
        public async Task<HttpResponseData> GetShots(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "players/faulty/getShots")] HttpRequestData req)
        {
            var errorResponse = req.CreateResponse();
            await errorResponse.WriteAsJsonAsync("This is an error\nfrom faulty player.");
            errorResponse.StatusCode = HttpStatusCode.InternalServerError;
            return errorResponse;
        }
    }
}
