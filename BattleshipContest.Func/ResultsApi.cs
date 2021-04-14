using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Core.Serialization;
using BattleshipContestFunc.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace BattleshipContestFunc
{
    public record ResultsGetDto(
        Guid Id, 
        string Name, 
        DateTime? LastMeasurement,
        double? AvgNumberOfShots,
        double? StdDev,
        string? GitHubUrl,
        string? UserNickName,
        string? PublicTwitter,
        string? PublicUrl);

    public partial class ResultsApi : ApiBase
    {
        private readonly IPlayerResultTable playerResultTable;
        private readonly IUsersTable usersTable;
        private readonly IPlayerTable playerTable;

        public ResultsApi(JsonSerializerOptions jsonOptions,
            JsonObjectSerializer jsonSerializer, IPlayerResultTable playerResultTable,
            IUsersTable usersTable, IPlayerTable playerTable)
            : base(jsonOptions, jsonSerializer)
        {
            this.playerResultTable = playerResultTable;
            this.usersTable = usersTable;
            this.playerTable = playerTable;
        }

        [Function("GetResults")]
        public async Task<HttpResponseData> Get(
                [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "results")] HttpRequestData req)
        {
            // Anonymous access is allowed

            // Get all players of current user 
            var results = await playerResultTable.Get();

            // Add data from player and user
            var responseResult = new List<ResultsGetDto>(results.Count);
            foreach(var item in results)
            {
                var player = await playerTable.GetSingle(Guid.Parse(item.RowKey));
                if (player == null) continue;
                var user = await usersTable.GetSingle(player!.Creator);
                responseResult.Add(new(Guid.Parse(item.RowKey), item.Name,
                    item.LastMeasurement, item.AvgNumberOfShots, item.StdDev,
                    player?.GitHubUrl, user?.NickName, user?.PublicTwitter, user?.PublicUrl));
            }

            responseResult = responseResult.OrderBy(r => r.AvgNumberOfShots).ToList();
            return await CreateResponse(req, responseResult);
        }
    }
}
