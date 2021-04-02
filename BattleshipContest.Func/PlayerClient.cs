using Microsoft.Extensions.Configuration;
using NBattleshipCodingContest.Logic;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BattleshipContestFunc
{
    public record ShotRequest(IEnumerable<SinglePlayerGameLogRecord> Shots, BoardIndex? LastShot, string Board);

    public class PlayerClient : IPlayerClient
    {
        private readonly IPlayerHttpClientFactory httpClientFactory;
        private readonly JsonSerializerOptions? jsonOptions;
        private static TimeSpan getReadyTimeout = TimeSpan.FromSeconds(15);
        private static TimeSpan getShotTimeout = TimeSpan.FromMilliseconds(500);

        public PlayerClient(IPlayerHttpClientFactory httpClientFactory, IConfiguration? configuration = null, JsonSerializerOptions? jsonOptions = null)
        {
            this.httpClientFactory = httpClientFactory;
            this.jsonOptions = jsonOptions;
            if (configuration != null)
            {
                if (getReadyTimeout == TimeSpan.Zero) getReadyTimeout = TimeSpan.FromMilliseconds(int.Parse(configuration["Timeouts:getReady"]));
                if (getShotTimeout == TimeSpan.Zero) getShotTimeout = TimeSpan.FromMilliseconds(int.Parse(configuration["Timeouts:getShot"]));
            }
        }

        internal static string BuildPathWithKey(string path, string? apiKey)
        {
            if (!string.IsNullOrEmpty(apiKey)) return path + $"?code={apiKey}";
            return path;
        }

        public async Task GetReady(string playerWebApiUrl, string? apiKey = null)
        {
            var client = httpClientFactory.GetHttpClient(playerWebApiUrl);
            await client.GetAsync(BuildPathWithKey("getReady", apiKey), getReadyTimeout);
        }

        public async Task<BoardIndex> GetShot(string playerWebApiUrl, ISinglePlayerGame game, string? apiKey = null)
        {
            var client = httpClientFactory.GetHttpClient(playerWebApiUrl);
            var url = BuildPathWithKey("getShot", apiKey);

            var shotRequest = new ShotRequest(game.Log, game.LastShot, game.ShootingBoard.ToShortString());
            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url, UriKind.Relative)
            };
            request.Content = new StringContent(JsonSerializer.Serialize(shotRequest, jsonOptions), Encoding.UTF8, MediaTypeNames.Application.Json);

            var response = await client.SendAsync(request, getShotTimeout);

            var responseShotString = await response.Content.ReadFromJsonAsync<string>();
            if (string.IsNullOrEmpty(responseShotString)) throw new InvalidShotException(null, "Player returned no or empty shot");
            if (!BoardIndex.TryParse(responseShotString, out var responseShot)) throw new InvalidShotException(responseShotString, "Player returned invalid shot");

            return responseShot;
        }
    }
}
