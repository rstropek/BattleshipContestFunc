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
    public class TimeoutPlayerApi
    {
        public TimeoutPlayerApi() { }

        [Function("GetReadyTimeoutPlayer")]
        [SuppressMessage("Performance", "CA1822", Justification = "Just a demo player")]
        public async Task<HttpResponseData> GetReady(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "players/timeout/getReady")] HttpRequestData req)
        {
            await Task.Delay(TimeSpan.FromMinutes(1));
            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function("GetShotTimeoutPlayer")]
        [SuppressMessage("Performance", "CA1822", Justification = "Just a demo player")]
        public HttpResponseData GetShot(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "players/timeout/getShot")] HttpRequestData req)
            => req.CreateResponse(HttpStatusCode.OK);
    }
}
