using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Threading.Tasks;

namespace BattleshipContestFunc
{
    public record ProblemDetails(string Type, string Title, string Detail) { }

    public static class HttpRequestDataExtensions
    {
        public static async Task<HttpResponseData> CreateValidationErrorResponse(this HttpRequestData req, string detail, JsonObjectSerializer jsonSerializer)
        {
            var response = req.CreateResponse();
            await response.WriteAsJsonAsync(new ProblemDetails(
                "https://battleshipcontest.net/errors/request-validation-error",
                "Your request parameters did not validate.",
                detail), jsonSerializer);
            response.StatusCode = HttpStatusCode.BadRequest;
            return response;
        }
    }
}
