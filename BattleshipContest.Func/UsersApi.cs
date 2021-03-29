using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using Azure.Core.Serialization;
using BattleshipContestFunc.Data;
using EmailValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace BattleshipContestFunc
{
    public record UserGetDto(string Subject, string NickName, string? PublicTwitter, string? PublicUrl);
    public record UserRegisterDto(string NickName, string Email, string? PublicTwitter, string? PublicUrl);

    public class UsersApi
    {
        private readonly IUsersTable usersTable;
        private readonly IMapper mapper;
        private readonly JsonSerializerOptions jsonOptions;
        private readonly JsonObjectSerializer jsonSerializer;
        private readonly IAuthorize authorize;

        public UsersApi(IUsersTable usersTable, IMapper mapper, JsonSerializerOptions jsonOptions,
            JsonObjectSerializer jsonSerializer, IAuthorize authorize)
        {
            this.usersTable = usersTable;
            this.mapper = mapper;
            this.jsonOptions = jsonOptions;
            this.jsonSerializer = jsonSerializer;
            this.authorize = authorize;
        }

        [Function("Me")]
        public async Task<HttpResponseData> Me(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/me")] HttpRequestData req)
        {
            // Verify authenticated user is present
            var subject = await authorize.TryGetSubject(req.Headers);
            if (subject == null)
            {
                return req.CreateResponse(HttpStatusCode.Unauthorized);
            }

            var user = await usersTable.GetSingle(subject);
            if (user == null)
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            var response = req.CreateResponse();
            await response.WriteAsJsonAsync(mapper.Map<User, UserGetDto>(user), jsonSerializer);
            return response;
        }

        [Function("Register")]
        public async Task<HttpResponseData> Add(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users")] HttpRequestData req)
        {
            // Verify authenticated user is present
            var subject = await authorize.TryGetSubject(req.Headers);
            if (subject == null)
            {
                return req.CreateResponse(HttpStatusCode.Unauthorized);
            }

            var existingUser = await usersTable.GetSingle(subject);
            if (existingUser != null)
            {
                return await req.CreateConflictErrorResponse($"User already registered.", jsonSerializer);
            }

            // Deserialize and verify DTO
            using var reader = new StreamReader(req.Body);
            UserRegisterDto? user;
            try
            {
                user = JsonSerializer.Deserialize<UserRegisterDto>(await reader.ReadToEndAsync(), jsonOptions);
            }
            catch (JsonException ex)
            {
                return await req.CreateValidationErrorResponse($"Could not parse request body ({ex.Message})", jsonSerializer);
            }

            if (user == null)
            {
                return await req.CreateValidationErrorResponse($"Missing user in request body.", jsonSerializer);
            }

            if (string.IsNullOrWhiteSpace(user.NickName))
            {
                return await req.CreateValidationErrorResponse($"Nick name must not be empty.", jsonSerializer);
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return await req.CreateValidationErrorResponse($"Email must not be empty.", jsonSerializer);
            }

            if (!EmailValidator.Validate(user.Email))
            {
                return await req.CreateValidationErrorResponse($"Email not valid.", jsonSerializer);
            }

            if (!string.IsNullOrEmpty(user.PublicUrl))
            {
                if (!Uri.TryCreate(user.PublicUrl, UriKind.Absolute, out var uri))
                {
                    return await req.CreateValidationErrorResponse($"Public profile URL must be a valid absolute URL.", jsonSerializer);
                }
                else
                {
                    user = user with { PublicUrl = Uri.EscapeUriString(uri.ToString()) };
                }
            }

            // Create data object from DTO
            var userToAdd = mapper.Map<UserRegisterDto, User>(user);
            userToAdd.RowKey = subject;

            // Store user
            await usersTable.Add(userToAdd);

            // Convert added user into DTO
            var userToReturn = mapper.Map<User, UserGetDto>(userToAdd);

            var response = req.CreateResponse();
            await response.WriteAsJsonAsync(userToReturn, jsonSerializer);
            response.StatusCode = HttpStatusCode.Created;
            return response;
        }
    }
}
