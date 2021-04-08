using Azure.Messaging.ServiceBus;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace BattleshipContestFunc
{
    public class MessageSender : IMessageSender
    {
        private readonly JsonSerializerOptions jsonOptions;

        public MessageSender(JsonSerializerOptions jsonOptions)
        {
            this.jsonOptions = jsonOptions;
        }

        public async Task SendMessage<T>(T content, string connectionString, string topicName, TimeSpan? delay = null)
        {
            var brokeredMessage = new ServiceBusMessage(JsonSerializer.Serialize(content, jsonOptions))
            {
                ContentType = "application/json; charset=utf-8",
                MessageId = new Random().Next(0, int.MaxValue).ToString()
            };

            if (delay != null) brokeredMessage.ScheduledEnqueueTime = DateTimeOffset.UtcNow + delay.Value;

            await using var client = new ServiceBusClient(connectionString);
            var sender = client.CreateSender(topicName);
            await sender.SendMessageAsync(brokeredMessage);
        }
    }
}
