using Azure.Core.Serialization;
using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace BattleshipContestFunc.Tests
{
    public class ApiBaseTests
    {
        private class Api : ApiBase
        {
            public Api() : base(new JsonSerializerOptions(), new JsonObjectSerializer()) { }
        }

        [Fact]
        public async Task DeserializeAndValidateBody()
        {
            var api = new Api();

            var data = new PlayerAddDto(Guid.Empty, "foo", "http://dummy.com");
            var responseMock = RequestResponseMocker.Create(data, api.jsonOptions);

            var (item, response) = await api.DeserializeAndValidateBody<PlayerAddDto>(responseMock.RequestMock.Object);

            Assert.NotNull(item);
            Assert.Null(response);
            Assert.Equal(data, item);
        }

        [Fact]
        public async Task DeserializeAndValidateBodyInvalid()
        {
            var api = new Api();

            var responseMock = RequestResponseMocker.Create("dummy {", api.jsonOptions);

            var (item, response) = await api.DeserializeAndValidateBody<PlayerAddDto>(responseMock.RequestMock.Object);

            Assert.Null(item);
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.BadRequest, responseMock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task CreateResponse()
        {
            var api = new Api();

            var data = new PlayerAddDto(Guid.Empty, "foo", "http://dummy.com");
            var responseMock = RequestResponseMocker.Create(data, api.jsonOptions);

            var response = await api.CreateResponse(responseMock.RequestMock.Object, "dummy", HttpStatusCode.BadRequest);

            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        private record DummyClass([property: Required] string? Required);

        [Fact]
        public void ValidateModelInvalid()
        {
            var api = new Api();
            var item = new DummyClass(null);
            var result = api.ValidateModel(item);
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public void ValidateModelValid()
        {
            var api = new Api();
            var item = new DummyClass("asdf");
            var result = api.ValidateModel(item);
            Assert.Null(result);
        }
    }
}
