using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BattleshipContestFunc
{
    public record ProblemDetails(string Type, string Title, string Detail) { }

    public abstract class ApiBase
    {
        protected internal readonly JsonSerializerOptions jsonOptions;
        protected internal readonly JsonObjectSerializer jsonSerializer;

        protected ApiBase(JsonSerializerOptions jsonOptions, JsonObjectSerializer jsonSerializer)
        {
            this.jsonOptions = jsonOptions;
            this.jsonSerializer = jsonSerializer;
        }

        protected internal async Task<HttpResponseData> CreateResponse<T>(HttpRequestData req, T payload, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var response = req.CreateResponse();
            await response.WriteAsJsonAsync(payload, jsonSerializer);
            response.StatusCode = statusCode;
            return response;
        }

        protected internal async Task<(T?, HttpResponseData?)> DeserializeAndValidateBody<T>(HttpRequestData req)
            where T: class
        {
            using var reader = new StreamReader(req.Body);
            T? item;
            try
            {
                item = JsonSerializer.Deserialize<T>(await reader.ReadToEndAsync(), jsonOptions);
            }
            catch (JsonException ex)
            {
                return (null, await CreateValidationError(req, $"Could not parse request body ({ex.Message})"));
            }

            if (item == null)
            {
                return (null, await CreateValidationError(req, $"Missing player in request body."));
            }

            return (item, null);
        }

        protected const string GuidParseErrorMessage = "Could not parse specified ID, must be a GUID with format xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx";

        public async Task<HttpResponseData> CreateValidationError(HttpRequestData req, string detail)
            => await CreateResponse(req, new ProblemDetails(
                    "https://battleshipcontest.net/errors/request-validation-error",
                    "Your request parameters did not validate.", detail),
                HttpStatusCode.BadRequest);

        public async Task<HttpResponseData> CreateConflictError(HttpRequestData req, string detail)
            => await CreateResponse(req, new ProblemDetails(
                    "https://battleshipcontest.net/errors/conflict",
                    "Your request could not be fulfilled because of a conflict.",
                    detail),
                HttpStatusCode.Conflict);

        public async Task<HttpResponseData> CreateDependencyError(HttpRequestData req, string detail)
            => await CreateResponse(req, new ProblemDetails(
                    "https://battleshipcontest.net/errors/failed-dependency",
                    "A request to a service we depend on failed.",
                    detail),
                HttpStatusCode.FailedDependency);

        [SuppressMessage("Performance", "CA1822", Justification = "Required for unit tests")]
        protected internal string? ValidateModel<T>(T instance) where T: notnull
        {
            var context = new ValidationContext(instance);
            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(instance, context, validationResults, true))
            {
                var resultBuilder = new StringBuilder();
                foreach(var item in validationResults)
                {
                    resultBuilder.Append(item.ErrorMessage);
                    resultBuilder.Append('\n');
                }

                return resultBuilder.ToString();
            }

            return null;
        }
    }
}
