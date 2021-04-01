using Microsoft.Extensions.Configuration;
using NBattleshipCodingContest.Logic;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BattleshipContestFunc
{
    public record ShotRequest(IEnumerable<SinglePlayerGameLogRecord> Shots, BoardIndex? LastShot, string Board);

    public class PlayerClient : IPlayerClient
    {
        private static readonly ConcurrentDictionary<string, HttpClient> playerHttpClients = new();
        private readonly IBoardFiller filler;
        private readonly JsonSerializerOptions jsonOptions;
        private static TimeSpan getReadyTimeout;
        private static TimeSpan getShotTimeout;

        public PlayerClient(IBoardFiller filler, IConfiguration configuration, JsonSerializerOptions jsonOptions)
        {
            this.filler = filler;
            this.jsonOptions = jsonOptions;
            if (getReadyTimeout == TimeSpan.Zero) getReadyTimeout = TimeSpan.FromMilliseconds(int.Parse(configuration["Timeouts:getReady"]));
            if (getShotTimeout == TimeSpan.Zero) getShotTimeout = TimeSpan.FromMilliseconds(int.Parse(configuration["Timeouts:getShot"]));
        }

        private static HttpClient GetHttpClient(string baseUrl)
        {
            var canonicalizedBaseUrl = CanonicalizeWebApiUrl(baseUrl);
            if (!playerHttpClients.ContainsKey(canonicalizedBaseUrl))
            {
                var client = new HttpClient { BaseAddress = new Uri(canonicalizedBaseUrl, UriKind.Absolute) };
                return playerHttpClients.GetOrAdd(canonicalizedBaseUrl, client);
            }

            return playerHttpClients[canonicalizedBaseUrl];
        }

        internal static string CanonicalizeWebApiUrl(string baseUrl)
        {
            if (baseUrl[^1] != '/') return baseUrl + '/';
            return baseUrl;
        }

        internal static string BuildPathWithKey(string path, string? apiKey)
        {
            if (!string.IsNullOrEmpty(apiKey)) return path + $"?code={apiKey}";
            return path;
        }

        public async Task GetReady(string playerWebApiUrl, string? apiKey = null)
        {
            var client = GetHttpClient(playerWebApiUrl);
            var url = BuildPathWithKey("getReady", apiKey);
            try
            {
                var cts = new CancellationTokenSource();
                cts.CancelAfter(getReadyTimeout);
                var response = await client.GetAsync(url, cts.Token);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new InvalidStatusCodeException(response);
                }
            }
            catch (OperationCanceledException) { throw new TimeoutException(); }
            catch (Exception ex) { throw new PlayerCommunicationException(null, ex); }
        }

        private async Task<BoardIndex> GetShot(string playerWebApiUrl, ISinglePlayerGame game, string? apiKey = null)
        {
            var client = GetHttpClient(playerWebApiUrl);
            var url = BuildPathWithKey("getShot", apiKey);

            var shotRequest = new ShotRequest(game.Log, game.LastShot, game.ShootingBoard.ToShortString());
            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url, UriKind.Relative)
            };
            request.Content = new StringContent(JsonSerializer.Serialize(shotRequest, jsonOptions), Encoding.UTF8, MediaTypeNames.Application.Json);
            var cts = new CancellationTokenSource();
            cts.CancelAfter(getShotTimeout);

            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(request);
            }
            catch (OperationCanceledException) { throw new TimeoutException(); }
            catch (Exception ex) { throw new PlayerCommunicationException(null, ex); }

            if (response.StatusCode != HttpStatusCode.OK) throw new InvalidStatusCodeException(response);

            var responseShotString = await response.Content.ReadFromJsonAsync<string>();
            if (string.IsNullOrEmpty(responseShotString)) throw new InvalidShotException(null, "Player returned no or empty shot");
            if (!BoardIndex.TryParse(responseShotString, out var responseShot)) throw new InvalidShotException(responseShotString, "Player returned invalid shot");

            return responseShot;
        }

        public async Task PlaySingleMoveInRandomGame(string playerWebApiUrl, string? apiKey = null)
        {
            var board = new BattleshipBoard();
            filler.Fill(BattleshipBoard.Ships, board);
            var game = new SinglePlayerGame(Guid.Empty, 0, board, new BoardContent(SquareContent.Unknown));

            var rand = new Random();
            while (true)
            {
                for (var i = rand.Next(10, 30); i > 0; i--)
                {
                    game.Shoot(new BoardIndex(rand.Next(10), rand.Next(10)));
                }

                if (game.GetGameState(BattleshipBoard.Ships) == SinglePlayerGameState.InProgress) break;
            }

            await GetShot(playerWebApiUrl, game, apiKey);
        }

        public async Task<int> PlayGame(string playerWebApiUrl, string? apiKey = null)
        {
            var board = new BattleshipBoard();
            filler.Fill(BattleshipBoard.Ships, board);
            var game = new SinglePlayerGame(Guid.Empty, 0, board, new BoardContent(SquareContent.Unknown));

            while (game.GetGameState(BattleshipBoard.Ships) == SinglePlayerGameState.InProgress)
            {
                var shot = await GetShot(playerWebApiUrl, game, apiKey);
                game.Shoot(shot);
            }

            return game.NumberOfShots;
        }
    }
}
