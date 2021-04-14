using Microsoft.Extensions.Configuration;
using NBattleshipCodingContest.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BattleshipContestFunc
{
    public record ShotRequest(Guid GameId, BoardIndex? LastShot, string Board);

    public record FinishedProtocolDto(Guid GameId, string Board, int NumberOfShots);

    public class PlayerClient : IPlayerClient
    {
        private readonly IPlayerHttpClientFactory httpClientFactory;
        private readonly JsonSerializerOptions? jsonOptions;
        private static TimeSpan getReadyTimeout = TimeSpan.Zero;
        private static TimeSpan getShotTimeout = TimeSpan.Zero;
        private static TimeSpan getShotsTimeout = TimeSpan.Zero;
        private static TimeSpan finishedTimeout = TimeSpan.Zero;

        public PlayerClient(IPlayerHttpClientFactory httpClientFactory, IConfiguration? configuration = null, JsonSerializerOptions? jsonOptions = null)
        {
            this.httpClientFactory = httpClientFactory;
            this.jsonOptions = jsonOptions;
            if (configuration != null)
            {
                if (getReadyTimeout == TimeSpan.Zero) getReadyTimeout = TimeSpan.FromMilliseconds(int.Parse(configuration["Timeouts:getReady"]));
                if (getShotTimeout == TimeSpan.Zero) getShotTimeout = TimeSpan.FromMilliseconds(int.Parse(configuration["Timeouts:getShot"]));
                if (getShotsTimeout == TimeSpan.Zero) getShotsTimeout = TimeSpan.FromMilliseconds(int.Parse(configuration["Timeouts:getShots"]));
                if (finishedTimeout == TimeSpan.Zero) finishedTimeout = TimeSpan.FromMilliseconds(int.Parse(configuration["Timeouts:finished"]));
            }
        }

        internal static string BuildPathWithKey(string path, string? apiKey, params KeyValuePair<string, string>[] parameters)
        {
            var pathBuilder = new StringBuilder(path);
            var first = true;
            if (!string.IsNullOrEmpty(apiKey))
            {
                first = false;
                pathBuilder.Append("?code=");
                pathBuilder.Append(apiKey);
            }

            foreach(var p in parameters)
            {
                if (first) pathBuilder.Append('?');
                else pathBuilder.Append('&');
                pathBuilder.Append(p.Key);
                pathBuilder.Append('=');
                pathBuilder.Append(p.Value);
            }

            return pathBuilder.ToString();
        }

        public async Task GetReady(string playerWebApiUrl, int numberOfGames, string? apiKey = null)
        {
            var client = httpClientFactory.GetHttpClient(playerWebApiUrl);
            await client.GetAsync(BuildPathWithKey("getReady", apiKey,
                new KeyValuePair<string, string>(nameof(numberOfGames), numberOfGames.ToString())), 
                getReadyTimeout);
        }

        public async Task<BoardIndex> GetShot(string playerWebApiUrl, ISinglePlayerGame game, string? apiKey = null)
        {
            var client = httpClientFactory.GetHttpClient(playerWebApiUrl);
            var url = BuildPathWithKey("getShot", apiKey);

            var shotRequest = new ShotRequest(game.GameId, game.LastShot, game.ShootingBoard.ToShortString());
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

        public record GameDescriptor();

        public async Task<IReadOnlyList<BoardIndex>> GetShots(string playerWebApiUrl, IEnumerable<ISinglePlayerGame> games, string? apiKey = null)
        {
            var client = httpClientFactory.GetHttpClient(playerWebApiUrl);
            var url = BuildPathWithKey("getShots", apiKey);

            var shotRequests = games.Select(g => new ShotRequest(g.GameId, g.LastShot, g.ShootingBoard.ToShortString())).ToArray();
            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url, UriKind.Relative)
            };
            request.Content = new StringContent(JsonSerializer.Serialize(shotRequests, jsonOptions), Encoding.UTF8, MediaTypeNames.Application.Json);

            var response = await client.SendAsync(request, getShotsTimeout);

            IEnumerable<string>? responseShotStrings;
            try
            {
                responseShotStrings = await response.Content.ReadFromJsonAsync<IEnumerable<string>>();
            }
            catch (JsonException ex)
            {
                throw new InvalidShotException(null, "Player returned invalid JSON", ex);
            }

            if (responseShotStrings == null) throw new InvalidShotException(null, "Player returned no or empty shot");
            if (responseShotStrings.Count() != shotRequests.Length) throw new InvalidShotException(null, "Wrong number of shots returned");
            if (responseShotStrings.Any(s => string.IsNullOrEmpty(s))) throw new InvalidShotException(null, "Player returned at least one empty shot");

            try
            {
                return responseShotStrings.Select(s => new BoardIndex(s)).ToArray();
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new InvalidShotException(null, "Player returned invalid board index", ex);
            }
        }
        public async Task Finished(string playerWebApiUrl, IEnumerable<ISinglePlayerGame> games, string? apiKey = null)
        {
            var client = httpClientFactory.GetHttpClient(playerWebApiUrl);
            var url = BuildPathWithKey("finished", apiKey);

            var finishedProtocol = games.Select(g => new FinishedProtocolDto(g.GameId, g.ShootingBoard.ToShortString(), g.NumberOfShots)).ToArray();
            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url, UriKind.Relative)
            };
            request.Content = new StringContent(JsonSerializer.Serialize(finishedProtocol, jsonOptions), Encoding.UTF8, MediaTypeNames.Application.Json);

            var response = await client.SendAsync(request, finishedTimeout);
        }
    }
}
