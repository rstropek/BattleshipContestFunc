using Microsoft.Azure.Functions.Worker.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BattleshipContestFunc
{
    public interface IAuthorize
    {
        Task<ClaimsPrincipal?> GetUser(HttpHeadersCollection headers);
        string? GetSubject(ClaimsPrincipal principal);
        Task<string?> TryGetSubject(HttpHeadersCollection headers);
    }
}
