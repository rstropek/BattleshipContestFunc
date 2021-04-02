using System;
using System.Threading.Tasks;

namespace BattleshipContestFunc.Data
{
    public interface IPlayerGameLeaseManager
    {
        Task<string> Acquire(Guid playerId, TimeSpan? duration = null);
        Task Renew(Guid playerId, string leaseId);
        Task Release(Guid playerId, string leaseId);
        Task Delete(Guid playerId, string leaseId);
    }
}
