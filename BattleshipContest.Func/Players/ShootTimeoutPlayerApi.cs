using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading.Tasks;

namespace BattleshipContestFunc.Players
{
    public class ShootTimeoutPlayerApi
    {
        public ShootTimeoutPlayerApi() { }

        [Function("GetReadyTimeoutPlayer2")]
        [SuppressMessage("Performance", "CA1822", Justification = "Just a demo player")]
        public HttpResponseData GetReady(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "players/timeout-shoot/getReady")] HttpRequestData req)
            => req.CreateResponse(HttpStatusCode.OK);

        [Function("GetShotsTimeoutPlayer2")]
        [SuppressMessage("Performance", "CA1822", Justification = "Just a demo player")]
        public async Task<HttpResponseData> GetShot(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "players/timeout-shoot/getShots")] HttpRequestData req)
        {
            await Task.Delay(TimeSpan.FromMinutes(1));
            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}
