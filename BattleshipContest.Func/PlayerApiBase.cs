using Azure.Core.Serialization;
using BattleshipContestFunc.Data;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace BattleshipContestFunc
{
    public abstract class PlayerApiBase : ApiBase
    {
        protected readonly IPlayerTable playerTable;

        protected PlayerApiBase(IPlayerTable playerTable, JsonSerializerOptions jsonOptions, JsonObjectSerializer jsonSerializer)
            : base(jsonOptions, jsonSerializer)
        {
            this.playerTable = playerTable;
        }

        protected async Task<(Player?, HttpResponseData?)> GetSingleOwning(HttpRequestData req, Guid id, string ownerSubject)
        {
            var entity = await playerTable.GetSingle(id);
            if (entity == null) return (null, req.CreateResponse(HttpStatusCode.NotFound));
            if (entity.Creator != ownerSubject) return (null, req.CreateResponse(HttpStatusCode.Forbidden));
            return (entity, null);
        }

        protected async Task<(Player?, HttpResponseData?)> GetSingleOwning(HttpRequestData req, string idString, string ownerSubject)
        {
            if (!Guid.TryParseExact(idString, "D", out var id)) return (null, await CreateValidationError(req, GuidParseErrorMessage));
            var (entity, errorResponse) = await GetSingleOwning(req, id, ownerSubject);
            if (entity == null) return (null, errorResponse!);
            return (entity, null);
        }

    }
}
