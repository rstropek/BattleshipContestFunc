using System;
using System.Threading.Tasks;

namespace BattleshipContestFunc
{
    public interface IMessageSender
    {
        Task SendMessage<T>(T content, string connectionString, string topicName, TimeSpan? delay = null);

        Task<T?> DecodeMessage<T>(byte[] content);
    }
}
