using BattleshipContestFunc.Data;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace BattleshipContestFunc.Tests
{
    public class UsersApi : IClassFixture<ApiConfigFixture>
    {
        private readonly ApiConfigFixture config;

        public UsersApi(ApiConfigFixture config)
        {
            this.config = config;
        }

        private BattleshipContestFunc.UsersApi CreateApi(Mock<IUsersTable> playerMock)
        {
            var authorizeMock = new Mock<IAuthorize>();
            authorizeMock.Setup(a => a.GetUser(It.IsAny<HttpHeadersCollection>()))
                .Returns(Task.FromResult<ClaimsPrincipal?>(null));
            return CreateApi(playerMock, authorizeMock);
        }

        private BattleshipContestFunc.UsersApi CreateApi(Mock<IUsersTable> playerMock, Mock<IAuthorize> authorize)
        {
            return new BattleshipContestFunc.UsersApi(playerMock.Object, config.Mapper,
                config.JsonOptions, config.Serializer, authorize.Object);
        }

        [Fact]
        public async Task Me()
        {
            var payload = new User("foo") { NickName = "bar", Email = "foo@bar.com", PublicTwitter = "@foobar", PublicUrl = "https://foobar.com/profile" };
            var usersMock = new Mock<IUsersTable>();
            usersMock.Setup(p => p.GetSingle("foo")).Returns(Task.FromResult<User?>(payload));

            var mock = RequestResponseMocker.Create();
            await CreateApi(usersMock, AuthorizeMocker.GetAuthorizeMock("foo")).Me(mock.RequestMock.Object);
            var resultPayload = JsonSerializer.Deserialize<UserGetDto>(mock.ResponseBodyAsString, config.JsonOptions);

            usersMock.Verify(p => p.GetSingle("foo"), Times.Once);
            Assert.Equal(HttpStatusCode.OK, mock.ResponseMock.Object.StatusCode);
            Assert.StartsWith("application/json", mock.Headers.First(h => h.Key == "Content-Type").Value.First());
            Assert.NotNull(resultPayload);
            Assert.Equal(payload.RowKey, resultPayload!.Subject);
            Assert.Equal(payload.NickName, resultPayload!.NickName);
            Assert.Equal(payload.PublicTwitter, resultPayload!.PublicTwitter);
            Assert.Equal(payload.PublicUrl, resultPayload!.PublicUrl);
        }

        [Fact]
        public async Task MeNotRegistered()
        {
            var usersMock = new Mock<IUsersTable>();
            usersMock.Setup(p => p.GetSingle("foo"));

            var mock = RequestResponseMocker.Create();
            await CreateApi(usersMock, AuthorizeMocker.GetAuthorizeMock("foo")).Me(mock.RequestMock.Object);

            usersMock.Verify(p => p.GetSingle("foo"), Times.Once);
            Assert.Equal(HttpStatusCode.NotFound, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task MeChecksAuthorization()
        {
            var usersMock = new Mock<IUsersTable>();
            usersMock.Setup(p => p.GetSingle(It.IsAny<string>()));

            var mock = RequestResponseMocker.Create();
            await CreateApi(usersMock).Me(mock.RequestMock.Object);

            usersMock.Verify(p => p.GetSingle(It.IsAny<string>()), Times.Never);
            Assert.Equal(HttpStatusCode.Unauthorized, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task RegisterChecksAuthorization()
        {
            var usersMock = new Mock<IUsersTable>();
            usersMock.Setup(p => p.Add(It.IsAny<User>()));

            var mock = RequestResponseMocker.Create();
            await CreateApi(usersMock).Add(mock.RequestMock.Object);

            usersMock.Verify(p => p.Add(It.IsAny<User>()), Times.Never);
            Assert.Equal(HttpStatusCode.Unauthorized, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task AlreadyRegistered()
        {
            var payload = new User("foo") { NickName = "bar", Email = "foo@bar.com", PublicTwitter = "@foobar", PublicUrl = "https://foobar.com/profile" };
            var usersMock = new Mock<IUsersTable>();
            usersMock.Setup(p => p.GetSingle("foo")).Returns(Task.FromResult<User?>(payload));
            usersMock.Setup(p => p.Add(It.IsAny<User>()));

            var mock = RequestResponseMocker.Create();
            await CreateApi(usersMock, AuthorizeMocker.GetAuthorizeMock("foo")).Add(mock.RequestMock.Object);

            usersMock.Verify(p => p.GetSingle("foo"), Times.Once);
            usersMock.Verify(p => p.Add(It.IsAny<User>()), Times.Never);
            Assert.Equal(HttpStatusCode.Conflict, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task RegisterInvalidBody()
        {
            var usersMock = new Mock<IUsersTable>();
            usersMock.Setup(p => p.Add(It.IsAny<User>()));

            var mock = RequestResponseMocker.Create("dummy {");
            await CreateApi(usersMock, AuthorizeMocker.GetAuthorizeMock("foo")).Add(mock.RequestMock.Object);

            usersMock.Verify(p => p.Add(It.IsAny<User>()), Times.Never);
            Assert.Equal(HttpStatusCode.BadRequest, mock.ResponseMock.Object.StatusCode);
        }

        private static UserRegisterDto GetEmptyRegisterDto() => new(string.Empty, string.Empty, null, null);

        [Fact]
        public async Task AddFailingValidation()
        {
            var userMock = new Mock<IUsersTable>();
            userMock.Setup(p => p.Add(It.IsAny<User>()));

            var mock = RequestResponseMocker.Create(GetEmptyRegisterDto(), config.JsonOptions);
            await CreateApi(userMock, AuthorizeMocker.GetAuthorizeMock("foo")).Add(mock.RequestMock.Object);

            userMock.Verify(p => p.Add(It.IsAny<User>()), Times.Never);
            Assert.Equal(HttpStatusCode.BadRequest, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task Add()
        {
            var usersMock = new Mock<IUsersTable>();
            usersMock.Setup(p => p.Add(It.IsAny<User>()));

            var mock = RequestResponseMocker.Create(
                new UserRegisterDto("foo", "foo@bar.com", "@foo", "https://someserver.com/api?x=a"), config.JsonOptions);
            await CreateApi(usersMock, AuthorizeMocker.GetAuthorizeMock("foo")).Add(mock.RequestMock.Object);
            var resultPayload = JsonSerializer.Deserialize<UserGetDto>(mock.ResponseBodyAsString, config.JsonOptions);

            usersMock.Verify(p => p.Add(It.IsAny<User>()), Times.Once);
            Assert.Equal(HttpStatusCode.Created, mock.ResponseMock.Object.StatusCode);
            Assert.StartsWith("application/json", mock.Headers.First(h => h.Key == "Content-Type").Value.First());
            Assert.NotNull(resultPayload);
            Assert.Equal("foo", resultPayload!.Subject);
            Assert.Equal("foo", resultPayload!.NickName);
            Assert.Equal("https://someserver.com/api?x=a", resultPayload!.PublicUrl);
            Assert.Equal("@foo", resultPayload!.PublicTwitter);
        }

        [Fact]
        public async Task AddEmptyProfileUrl()
        {
            var usersMock = new Mock<IUsersTable>();
            usersMock.Setup(p => p.Add(It.IsAny<User>()));

            var mock = RequestResponseMocker.Create(
                new UserRegisterDto("foo", "foo@bar.com", "@foo", ""), config.JsonOptions);
            await CreateApi(usersMock, AuthorizeMocker.GetAuthorizeMock("foo")).Add(mock.RequestMock.Object);
            var resultPayload = JsonSerializer.Deserialize<UserGetDto>(mock.ResponseBodyAsString, config.JsonOptions);

            usersMock.Verify(p => p.Add(It.IsAny<User>()), Times.Once);
            Assert.Equal(HttpStatusCode.Created, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task PatchChecksAuthorization()
        {
            var mock = RequestResponseMocker.Create();
            await CreateApi(new Mock<IUsersTable>()).Patch(mock.RequestMock.Object);

            Assert.Equal(HttpStatusCode.Unauthorized, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task PatchInvalidBody()
        {
            var playerMock = new Mock<IUsersTable>();
            playerMock.Setup(p => p.Replace(It.IsAny<User>()));

            var mock = RequestResponseMocker.Create("dummy {");
            await CreateApi(playerMock, AuthorizeMocker.GetAuthorizeMock("foo")).Patch(mock.RequestMock.Object);

            playerMock.Verify(p => p.Replace(It.IsAny<User>()), Times.Never);
            Assert.Equal(HttpStatusCode.BadRequest, mock.ResponseMock.Object.StatusCode);
        }
        private static UserRegisterDto GetEmptyAddDto() => new(string.Empty, string.Empty, string.Empty, string.Empty);

        [Fact]
        public async Task PatchFailingValidation()
        {
            var playerMock = new Mock<IUsersTable>();
            playerMock.Setup(p => p.Replace(It.IsAny<User>()));

            var mock = RequestResponseMocker.Create(GetEmptyAddDto() with { NickName = "asdf" }, config.JsonOptions);
            await CreateApi(playerMock, AuthorizeMocker.GetAuthorizeMock("foo")).Patch(mock.RequestMock.Object);

            playerMock.Verify(p => p.Replace(It.IsAny<User>()), Times.Never);
            Assert.Equal(HttpStatusCode.BadRequest, mock.ResponseMock.Object.StatusCode);
        }

        private static User GetFilledUser() => new("foo")
        { 
            NickName = "asdf", 
            Email = "foo@bar.com", 
            PublicTwitter = "@foo", 
            PublicUrl = "https://asdf.com/asdf" 
        };

        [Fact]
        public async Task NoUpdateIfNothingChanged()
        {
            var data = GetFilledUser();
            var playerMock = new Mock<IUsersTable>();
            playerMock.Setup(p => p.GetSingle("foo")).Returns(Task.FromResult<User?>(data));
            playerMock.Setup(p => p.Replace(It.IsAny<User>()));

            var mock = RequestResponseMocker.Create(new UserPatchDto(data.NickName, data.Email, data.PublicTwitter, data.PublicUrl), config.JsonOptions);
            await CreateApi(playerMock, AuthorizeMocker.GetAuthorizeMock("foo")).Patch(mock.RequestMock.Object);

            playerMock.Verify(p => p.Replace(It.IsAny<User>()), Times.Never);
            Assert.Equal(HttpStatusCode.OK, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task UpdateEmptyName()
        {
            var data = GetFilledUser();
            var playerMock = new Mock<IUsersTable>();
            playerMock.Setup(p => p.GetSingle("foo")).Returns(Task.FromResult<User?>(data));
            playerMock.Setup(p => p.Replace(It.IsAny<User>()));

            var mock = RequestResponseMocker.Create(new UserPatchDto(NickName: ""), config.JsonOptions);
            await CreateApi(playerMock, AuthorizeMocker.GetAuthorizeMock("foo")).Patch(mock.RequestMock.Object);

            playerMock.Verify(p => p.Replace(It.IsAny<User>()), Times.Never);
            Assert.Equal(HttpStatusCode.BadRequest, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task Update()
        {
            var data = GetFilledUser();
            var playerMock = new Mock<IUsersTable>();
            playerMock.Setup(p => p.GetSingle("foo")).Returns(Task.FromResult<User?>(data));
            playerMock.Setup(p => p.Replace(It.IsAny<User>()));

            var mock = RequestResponseMocker.Create(new UserPatchDto("as", "foo2@bar2.com", "@bar", "https://foo.bar.at/asdf"), config.JsonOptions);
            await CreateApi(playerMock, AuthorizeMocker.GetAuthorizeMock("foo")).Patch(mock.RequestMock.Object);

            Expression<Func<User, bool>> verify = p => p.NickName == "as" && p.Email == "foo2@bar2.com"
                && p.PublicTwitter == "@bar" && p.PublicUrl == "https://foo.bar.at/asdf";

            playerMock.Verify(p => p.Replace(It.Is(verify)), Times.Once);
            Assert.Equal(HttpStatusCode.OK, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task UpdateSingleField()
        {
            var data = GetFilledUser();
            var playerMock = new Mock<IUsersTable>();
            playerMock.Setup(p => p.GetSingle("foo")).Returns(Task.FromResult<User?>(data));
            playerMock.Setup(p => p.Replace(It.IsAny<User>()));

            var mock = RequestResponseMocker.Create(new UserPatchDto(null, "foo2@bar2.com"), config.JsonOptions);
            await CreateApi(playerMock, AuthorizeMocker.GetAuthorizeMock("foo")).Patch(mock.RequestMock.Object);

            Expression<Func<User, bool>> verify = p => p.NickName == data.NickName && p.Email == "foo2@bar2.com"
                && p.PublicTwitter == data.PublicTwitter && p.PublicUrl == data.PublicUrl;

            playerMock.Verify(p => p.Replace(It.Is(verify)), Times.Once);
            Assert.Equal(HttpStatusCode.OK, mock.ResponseMock.Object.StatusCode);
        }
    }
}
