using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace BattleshipContestFunc
{
    public interface IPlayerHttpClient
    {
        Task<HttpResponseMessage> GetAsync(string url, TimeSpan timeout);

        Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, TimeSpan timeout);
    }
}
