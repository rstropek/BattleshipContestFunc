using Microsoft.Azure.Cosmos.Table;

namespace BattleshipContestFunc.Data
{
    public class User : TableEntity
    {
        public User()
        {
            PartitionKey = Constants.UsersPartitionKey;
        }

        public User(string subject) : this()
        {
            RowKey = subject;
        }

        public string NickName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? PublicTwitter { get; set; }

        public string? PublicUrl { get; set; }
    }
}
