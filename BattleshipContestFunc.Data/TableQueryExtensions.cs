using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Cosmos.Table.Queryable;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BattleshipContestFunc.Data
{
    public static class TableQueryExtensions
    {
        public async static Task<List<T>> ToListAsync<T>(this TableQuery<T> tableQuery)
        {
            var nextQuery = tableQuery;
            var continuationToken = default(TableContinuationToken);
            var result = new List<T>();

            do
            {
                var queryResult = await nextQuery.ExecuteSegmentedAsync(continuationToken);

                result.Capacity += queryResult.Results.Count;
                result.AddRange(queryResult.Results);

                continuationToken = queryResult.ContinuationToken;
                if (continuationToken != null && tableQuery.TakeCount.HasValue)
                {
                    var itemsToLoad = tableQuery.TakeCount.Value - result.Count;
                    nextQuery = itemsToLoad > 0 ? tableQuery.Take<T>(itemsToLoad).AsTableQuery() : null;
                }
            }
            while (continuationToken != null && nextQuery != null);

            return result;
        }
    }
}
