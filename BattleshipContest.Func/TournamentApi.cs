using Azure.Core.Serialization;
using BattleshipContestFunc.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using NBattleshipCodingContest.Logic;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace BattleshipContestFunc
{
    public class TournamentApi
    {
        private readonly IPlayerTable playerTable;
        private readonly JsonSerializerOptions jsonOptions;
        private readonly JsonObjectSerializer jsonSerializer;

        public TournamentApi(IPlayerTable playerTable, JsonSerializerOptions jsonOptions, JsonObjectSerializer jsonSerializer)
        {
            this.playerTable = playerTable;
            this.jsonOptions = jsonOptions;
            this.jsonSerializer = jsonSerializer;
        }
    }
}
