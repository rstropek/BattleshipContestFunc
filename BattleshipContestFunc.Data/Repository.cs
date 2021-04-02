using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace BattleshipContestFunc.Data
{
    public class Repository : IRepository
    {
        private readonly IConfiguration configuration;
        private readonly Lazy<CloudStorageAccount> cloudStorageAccount;
        private readonly Lazy<CloudTableClient> tableClient;

        public Repository(IConfiguration configuration)
        {
            this.configuration = configuration;
            cloudStorageAccount = new(CreateStorageAccount, true);
            tableClient = new(CreateTableClient, true);
        }

        private CloudStorageAccount CreateStorageAccount()
            => CloudStorageAccount.Parse(configuration["ContestStoreConnectionString"]);

        private CloudTableClient CreateTableClient()
            => cloudStorageAccount.Value.CreateCloudTableClient(new TableClientConfiguration());

        public async Task<CloudTable> EnsureTableCreated(string tableName)
        {
            var table = tableClient.Value.GetTableReference(tableName);
            await table.CreateIfNotExistsAsync();
            return table;
        }

        public async Task<CloudTable?> GetTable(string tableName)
        {
            var table = tableClient.Value.GetTableReference(tableName);
            if (await table.ExistsAsync()) return table;
            return null;
        }

        public async Task EnsureTableDeleted(string tableName)
        {
            var table = tableClient.Value.GetTableReference(tableName);
            await table.DeleteIfExistsAsync();
        }
    }
}
