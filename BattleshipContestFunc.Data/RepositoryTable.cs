using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Cosmos.Table.Queryable;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace BattleshipContestFunc.Data
{
    public class RepositoryTable<TTable, TPartitionKey, TRowKey> : IRepositoryTable<TTable, TPartitionKey, TRowKey>
        where TTable : TableEntity, new()
        where TPartitionKey : notnull, IEquatable<TPartitionKey>
        where TRowKey : notnull
    {
        private readonly ILogger<RepositoryTable<TTable, TPartitionKey, TRowKey>> logger;
        private readonly IRepository repository;
        private readonly string tableName;
        private readonly TPartitionKey? partitionKey;
        private readonly string? partitionKeyString;

        public RepositoryTable(ILogger<RepositoryTable<TTable, TPartitionKey, TRowKey>> logger,
            IRepository repository, string tableName)
        {
            this.logger = logger;
            this.repository = repository;
            this.tableName = tableName;
        }

        public RepositoryTable(ILogger<RepositoryTable<TTable, TPartitionKey, TRowKey>> logger,
            IRepository repository, string tableName, TPartitionKey? partitionKey)
            : this(logger, repository, tableName)
        {
            this.partitionKey = partitionKey;
            partitionKeyString = partitionKey?.ToString();
        }

        public async Task<TTable?> Add(TTable item)
        {
            if (partitionKey != null && item.PartitionKey != partitionKeyString)
            {
                throw new InvalidPartitionKeyException($"Partition key of item to add/replace does not match specified partition key of table. " +
                    $"Specified partition key is {partitionKeyString}, item's partition key is {item.PartitionKey}. They have to be identical.");
            }

            var table = await repository.EnsureTableCreated(tableName);
            var op = TableOperation.Insert(item);
            var result = await table.ExecuteAsync(op);
            return result.Result as TTable;
        }

        public async Task<List<TTable>> Get(Expression<Func<TTable, bool>>? predicate = null)
        {
            if (partitionKey == null)
            {
                throw new InvalidPartitionKeyException($"Cannot use parameterless version of {nameof(Get)} because no partition key has been set. " +
                    $"Specify partition key in constructor or in call to {nameof(Get)}.");
            }

            return await Get(partitionKey, predicate);
        }

        public async Task<List<TTable>> Get(TPartitionKey partitionKey, Expression<Func<TTable, bool>>? predicate = null)
        {
            if (this.partitionKey != null && !partitionKey.Equals(this.partitionKey))
            {
                logger.LogWarning($"Partition keys do not match. " +
                    $"Specified partition key is {partitionKeyString}, selected partition key is {partitionKey}.");
            }

            var table = await repository.EnsureTableCreated(tableName);
            var query = table.CreateQuery<TTable>()
                .Where(c => c.PartitionKey == partitionKey.ToString());
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return await query.AsTableQuery().ToListAsync();
        }

        public async Task<TTable?> GetSingle(TRowKey rowKey)
        {
            if (partitionKey == null)
            {
                throw new InvalidPartitionKeyException($"Cannot use parameterless version of {nameof(GetSingle)} because no partition key has been set. " +
                    $"Specify partition key in constructor or in call to {nameof(GetSingle)}.");
            }

            return await GetSingle(partitionKey, rowKey);
        }

        public async Task<TTable?> GetSingle(TPartitionKey partitionKey, TRowKey rowKey)
        {
            if (this.partitionKey != null && !partitionKey.Equals(this.partitionKey))
            {
                logger.LogWarning($"Partition keys do not match. " +
                    $"Specified partition key is {partitionKeyString}, selected partition key is {partitionKey}.");
            }

            return await GetSingleImpl(partitionKey, rowKey);
        }

        private async Task<TTable?> GetSingleImpl(TPartitionKey partitionKey, TRowKey rowKey)
        {
            var table = await repository.EnsureTableCreated(tableName);
            var op = TableOperation.Retrieve<TTable>(partitionKey.ToString(), rowKey.ToString());
            var result = await table.ExecuteAsync(op);
            return result.Result as TTable;
        }

        public async Task Delete(TRowKey rowKey)
        {
            if (partitionKey == null)
            {
                throw new InvalidPartitionKeyException($"Cannot use parameterless version of {nameof(Delete)} because no partition key has been set. " +
                    $"Specify partition key in constructor or in call to {nameof(Delete)}.");
            }

            await Delete(partitionKey, rowKey);
        }

        public async Task Delete(TPartitionKey partitionKey, TRowKey rowKey)
        {
            if (this.partitionKey != null && !partitionKey.Equals(this.partitionKey))
            {
                logger.LogWarning($"Partition keys do not match. " +
                    $"Specified partition key is {partitionKeyString}, partition key for deletion is {partitionKey}.");
            }

            var table = await repository.EnsureTableCreated(tableName);
            var entity = await GetSingleImpl(partitionKey, rowKey);
            if (entity != null)
            {
                var op = TableOperation.Delete(entity);
                await table.ExecuteAsync(op);
            }
        }
    }
}
