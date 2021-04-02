using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BattleshipContestFunc
{
    public class PlayerHttpClient : IPlayerHttpClient
    {
        internal readonly HttpClient client;

        public PlayerHttpClient(HttpClient client)
        {
            this.client = client;
        }

        private async Task<HttpResponseMessage> ExecuteAsync(Func<CancellationToken, Task<HttpResponseMessage>> body, TimeSpan timeout)
        {
            HttpResponseMessage response;
            try
            {
                // Setup timeout
                var cts = new CancellationTokenSource();
                cts.CancelAfter(timeout);

                // Execute HTTP request
                response = await body(cts.Token);
            }
            catch (OperationCanceledException) { throw new TimeoutException(); }
            catch (Exception ex) { throw new PlayerCommunicationException(null, ex); }

            // We require OK as status code
            if (response.StatusCode != HttpStatusCode.OK) throw new InvalidStatusCodeException(response);

            return response;
        }

        public async Task<HttpResponseMessage> GetAsync(string url, TimeSpan timeout)
            => await ExecuteAsync(async (ct) => await client.GetAsync(url, ct), timeout);

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, TimeSpan timeout)
            => await ExecuteAsync(async (ct) => await client.SendAsync(message, ct), timeout);
    }
}
