using Microsoft.Extensions.Logging;

namespace BattleshipContestFunc.Data
{
    public interface IUsersTable : IRepositoryTable<User, string, string>
    {
    }

    public class UsersTable : RepositoryTable<User, string, string>, IUsersTable
    {
        public UsersTable(ILogger<RepositoryTable<User, string, string>> logger, IRepository repository)
            : base(logger, repository, Constants.UsersTable, Constants.UsersPartitionKey)
        {
        }
    }
}
