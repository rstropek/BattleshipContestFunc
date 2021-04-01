using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace BattleshipContestFunc.Data
{
    public interface IRepositoryTable<TTable, TPartitionKey, TRowKey>
        where TTable : TableEntity, new()
        where TPartitionKey : notnull, IEquatable<TPartitionKey>
        where TRowKey : notnull
    {
        Task<TTable?> Add(TTable item);
        Task<List<TTable>> Get(Expression<Func<TTable, bool>>? predicate = null);
        Task<List<TTable>> Get(TPartitionKey partitionKey, Expression<Func<TTable, bool>>? predicate = null);
        Task<TTable?> GetSingle(TRowKey rowKey);
        Task<TTable?> GetSingle(TPartitionKey partitionKey, TRowKey rowKey);
        Task Delete(TRowKey rowKey);
        Task Delete(TPartitionKey partitionKey, TRowKey rowKey);
        Task DeletePartition(TPartitionKey partitionKey);
        Task Delete(TTable entity);
        Task<TTable?> Replace(TTable item);
    }
}
