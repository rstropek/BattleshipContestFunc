using System;
using System.Collections.Concurrent;
using System.Net.Http;

namespace BattleshipContestFunc
{
    public class PlayerHttpClientFactory : IPlayerHttpClientFactory
    {
        private readonly ConcurrentDictionary<string, PlayerHttpClient> playerHttpClients = new();

        public IPlayerHttpClient GetHttpClient(string baseUrl)
        {
            var canonicalizedBaseUrl = CanonicalizeWebApiUrl(baseUrl);
            if (!playerHttpClients.ContainsKey(canonicalizedBaseUrl))
            {
                var client = new HttpClient { BaseAddress = new Uri(canonicalizedBaseUrl, UriKind.Absolute) };
                var playerClient = new PlayerHttpClient(client);
                return playerHttpClients.GetOrAdd(canonicalizedBaseUrl, playerClient);
            }

            return playerHttpClients[canonicalizedBaseUrl];
        }

        private static string CanonicalizeWebApiUrl(string baseUrl)
        {
            if (baseUrl[^1] != '/') return baseUrl + '/';
            return baseUrl;
        }
    }
}
