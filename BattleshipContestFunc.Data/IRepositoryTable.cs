using Microsoft.Azure.Cosmos.Table;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BattleshipContestFunc.Data
{
    public interface IRepositoryTable<TTable, TPartitionKey, TRowKey>
        where TTable : TableEntity, new()
        where TPartitionKey : notnull, IEquatable<TPartitionKey>
        where TRowKey : notnull
    {
        Task<TTable?> Add(TTable item);
        Task<IQueryable<TTable>> Get();
        Task<IQueryable<TTable>> Get(TPartitionKey partitionKey);
        Task<TTable?> GetSingle(TRowKey rowKey);
        Task<TTable?> GetSingle(TPartitionKey partitionKey, TRowKey rowKey);
        Task Delete(TRowKey rowKey);
        Task Delete(TPartitionKey partitionKey, TRowKey rowKey);
    }
}
