using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BattleshipContestFunc.Data
{
    public class PlayerGameLeaseManager : IPlayerGameLeaseManager
    {
        private readonly Lazy<BlobServiceClient> serviceClient;
        private readonly IConfiguration configuration;

        public PlayerGameLeaseManager(IConfiguration configuration)
        {
            this.configuration = configuration;
            serviceClient = new(CreateBlobClient, true);
        }

        private BlobServiceClient CreateBlobClient()
            => new(configuration["ContestStoreConnectionString"]);

        private async Task<BlobContainerClient> EnsureContainerCreated(string? containerName = null)
        {
            containerName ??= "gameleases";
            var containerClient = serviceClient.Value.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
            return containerClient;
        }

        private async Task<BlockBlobClient> EnsureBlobCreated(string blobName)
        {
            var containerClient = await EnsureContainerCreated();
            var blobClient = containerClient.GetBlockBlobClient(blobName);
            if (!await blobClient.ExistsAsync())
            {
                using var ms = new MemoryStream();
                await blobClient.UploadAsync(ms);
            }

            return blobClient;
        }

        private async Task EnsureBlobDeleted(string blobName)
        {
            var containerClient = await EnsureContainerCreated();
            var blobClient = containerClient.GetBlockBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }

        public async Task<string> Acquire(Guid playerId, TimeSpan? duration = null)
        {
            duration ??= TimeSpan.FromSeconds(60);
            var blobClient = await EnsureBlobCreated(playerId.ToString());
            var leaseClient = blobClient.GetBlobLeaseClient();
            var lease = await leaseClient.AcquireAsync(duration.Value);
            return lease.Value.LeaseId;
        }

        public async Task Renew(Guid playerId, string leaseId)
        {
            var blobClient = await EnsureBlobCreated(playerId.ToString());
            var leaseClient = blobClient.GetBlobLeaseClient(leaseId);
            await leaseClient.RenewAsync();
        }

        public async Task Release(Guid playerId, string leaseId)
        {
            var blobClient = await EnsureBlobCreated(playerId.ToString());
            var leaseClient = blobClient.GetBlobLeaseClient(leaseId);
            await leaseClient.ReleaseAsync();
        }

        public async Task Delete(Guid playerId, string leaseId)
        {
            string blobName = playerId.ToString();
            var blobClient = await EnsureBlobCreated(blobName);
            var leaseClient = blobClient.GetBlobLeaseClient(leaseId);
            await leaseClient.ReleaseAsync();
            await EnsureBlobDeleted(blobName);
        }
    }
}
