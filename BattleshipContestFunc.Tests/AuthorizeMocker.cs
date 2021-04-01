using Microsoft.Azure.Functions.Worker.Http;
using Moq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BattleshipContestFunc.Tests
{
    public static class AuthorizeMocker
    {
        public static Mock<IAuthorize> GetAuthorizeMock(string userSubject)
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new(ClaimTypes.NameIdentifier, userSubject)
            }));
            var authorizeMock = new Mock<IAuthorize>();
            authorizeMock.Setup(a => a.TryGetSubject(It.IsAny<HttpHeadersCollection>()))
                .Returns(Task.FromResult<string?>(userSubject));
            return authorizeMock;
        }

        public static Mock<IAuthorize> GetUnauthorizedMock()
        {
            var authorizeMock = new Mock<IAuthorize>();
            authorizeMock.Setup(a => a.TryGetSubject(It.IsAny<HttpHeadersCollection>()))
                .Returns(Task.FromResult<string?>(null));
            return authorizeMock;
        }
    }
}
