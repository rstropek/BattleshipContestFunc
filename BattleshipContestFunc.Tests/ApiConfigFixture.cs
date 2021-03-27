using AutoMapper;
using Azure.Core.Serialization;
using NBattleshipCodingContest.Logic;
using System.Text.Json;

namespace BattleshipContestFunc.Tests
{
    public class ApiConfigFixture
    {
        public ApiConfigFixture()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            Mapper = config.CreateMapper();

            JsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            JsonOptions.Converters.Add(new BoardContentJsonConverter());
            JsonOptions.Converters.Add(new BoardIndexJsonConverter());
            Serializer = new JsonObjectSerializer(JsonOptions);
        }

        public IMapper Mapper { get; }
        public JsonSerializerOptions JsonOptions { get; }
        public JsonObjectSerializer Serializer { get; }
    }
}
