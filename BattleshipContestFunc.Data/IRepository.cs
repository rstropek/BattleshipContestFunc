using Microsoft.Azure.Cosmos.Table;
using System.Threading.Tasks;

namespace BattleshipContestFunc.Data
{
    public interface IRepository
    {
        Task<CloudTable> EnsureTableCreated(string tableName);
        Task EnsureTableDeleted(string tableName);
        Task<CloudTable?> GetTable(string tableName);
    }
}
