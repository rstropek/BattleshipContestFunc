using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading.Tasks;

namespace BattleshipContestFunc.Players
{
    public class GetReadyTimeoutPlayerApi
    {
        public GetReadyTimeoutPlayerApi() { }

        [Function("GetReadyTimeoutPlayer1")]
        [SuppressMessage("Performance", "CA1822", Justification = "Just a demo player")]
        public async Task<HttpResponseData> GetReady(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "players/timeout-getReady/getReady")] HttpRequestData req)
        {
            await Task.Delay(TimeSpan.FromMinutes(1));
            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function("GetShotsTimeoutPlayer1")]
        [SuppressMessage("Performance", "CA1822", Justification = "Just a demo player")]
        public HttpResponseData GetShot(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "players/timeout-getReady/getShots")] HttpRequestData req)
            => req.CreateResponse(HttpStatusCode.InternalServerError);
    }
}
