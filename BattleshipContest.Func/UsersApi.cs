using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using Azure.Core.Serialization;
using BattleshipContestFunc.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace BattleshipContestFunc
{
    public record UserGetDto(string Subject, string NickName, string? PublicTwitter, string? PublicUrl);
    public record UserPatchDto(
        string? NickName = null, 
        [property: EmailAddress] string? Email = null,
        string? PublicTwitter = null, 
        [property: AbsoluteUri] string? PublicUrl = null);
    public record UserRegisterDto(
        [property: Required] string NickName,
        [property: Required][property: EmailAddress] string Email, 
        string? PublicTwitter,
        [property: AbsoluteUri] string? PublicUrl);

    public class UsersApi : ApiBase
    {
        private readonly IUsersTable usersTable;
        private readonly IMapper mapper;
        private readonly IAuthorize authorize;

        public UsersApi(IUsersTable usersTable, IMapper mapper, JsonSerializerOptions jsonOptions,
            JsonObjectSerializer jsonSerializer, IAuthorize authorize)
            : base(jsonOptions, jsonSerializer)
        {
            this.usersTable = usersTable;
            this.mapper = mapper;
            this.authorize = authorize;
        }

        [Function("Me")]
        public async Task<HttpResponseData> Me(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/me")] HttpRequestData req)
        {
            // Verify authenticated user is present
            var subject = await authorize.TryGetSubject(req.Headers);
            if (subject == null) return req.CreateResponse(HttpStatusCode.Unauthorized);

            var user = await usersTable.GetSingle(subject);
            if (user == null) return req.CreateResponse(HttpStatusCode.NotFound);

            return await CreateResponse(req, mapper.Map<User, UserGetDto>(user));
        }

        [Function("Register")]
        public async Task<HttpResponseData> Add(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users")] HttpRequestData req)
        {
            // Verify authenticated user is present
            var subject = await authorize.TryGetSubject(req.Headers);
            if (subject == null) return req.CreateResponse(HttpStatusCode.Unauthorized);

            var existingUser = await usersTable.GetSingle(subject);
            if (existingUser != null) return await CreateConflictError(req, $"User already registered.");

            // Deserialize and verify DTO
            var (user, errorResponse) = await DeserializeAndValidateBody<UserRegisterDto>(req);
            if (user == null) return errorResponse!;

            if (user.PublicUrl == string.Empty) user = user with { PublicUrl = null };

            var validationError = ValidateModel(user);
            if (validationError != null) return await CreateValidationError(req, validationError);

            // Create data object from DTO
            var userToAdd = mapper.Map<UserRegisterDto, User>(user);
            userToAdd.RowKey = subject;

            // Store user
            await usersTable.Add(userToAdd);

            // Convert added user into DTO
            var userToReturn = mapper.Map<User, UserGetDto>(userToAdd);

            return await CreateResponse(req, userToReturn, HttpStatusCode.Created);
        }

        [Function("PatchUser")]
        public async Task<HttpResponseData> Patch(
            [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "users/me")] HttpRequestData req)
        {
            // Verify authenticated user is present
            var subject = await authorize.TryGetSubject(req.Headers);
            if (subject == null) return req.CreateResponse(HttpStatusCode.Unauthorized);

            var (user, errorResponse) = await DeserializeAndValidateBody<UserPatchDto>(req);
            if (user == null) return errorResponse!;

            var validationError = ValidateModel(user);
            if (validationError != null) return await CreateValidationError(req, validationError);

            var entity = await usersTable.GetSingle(subject);
            if (entity == null) return req.CreateResponse(HttpStatusCode.NotFound);

            var update = false;
            if (user.NickName != null && user.NickName != entity.NickName)
            {
                if (user.NickName.Length == 0) return await CreateValidationError(req, $"Nickname must not be empty.");
                entity.NickName = user.NickName;
                update = true;
            }

            if (user.Email != null && user.Email != entity.Email)
            {
                if (user.Email.Length == 0) return await CreateValidationError(req, $"Email must not be empty.");
                entity.Email = user.Email;
                update = true;
            }

            if (user.PublicTwitter != null && user.PublicTwitter != entity.PublicTwitter)
            {
                entity.PublicTwitter = user.PublicTwitter.Length == 0 ? null : user.PublicTwitter;
                update = true;
            }

            if (user.PublicUrl != null && user.PublicUrl != entity.PublicUrl)
            {
                entity.PublicUrl = user.PublicUrl.Length == 0 ? null : user.PublicUrl;
                update = true;
            }

            if (update) await usersTable.Replace(entity);

            return await CreateResponse(req, mapper.Map<User, UserGetDto>(entity));
        }
    }
}
