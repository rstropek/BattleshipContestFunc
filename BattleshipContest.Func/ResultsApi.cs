using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
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
        double? AvgNumberOfShots);

    public partial class ResultsApi : ApiBase
    {
        private readonly IMapper mapper;
        private readonly IPlayerResultTable playerResultTable;

        public ResultsApi(IMapper mapper, JsonSerializerOptions jsonOptions,
            JsonObjectSerializer jsonSerializer, IPlayerResultTable playerResultTable)
            : base(jsonOptions, jsonSerializer)
        {
            this.mapper = mapper;
            this.playerResultTable = playerResultTable;
        }

        [Function("GetResults")]
        public async Task<HttpResponseData> Get(
                [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "results")] HttpRequestData req)
        {
            // Anonymous access is allowed

            // Get and return all players of current user 
            var results = mapper.Map<List<PlayerResult>, List<ResultsGetDto>>(await playerResultTable.Get());
            results = results.OrderBy(r => r.AvgNumberOfShots).ToList();
            return await CreateResponse(req, results);
        }
    }
}
