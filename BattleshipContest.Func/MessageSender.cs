using Azure.Messaging.ServiceBus;
using System;
using System.IO;
using System.IO.Compression;
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

        internal async Task<byte[]> Compress<T>(T content)
        {
            using var memoryStream = new MemoryStream();
            using (var compressionStream = new GZipStream(memoryStream, CompressionLevel.Optimal))
            {
                await JsonSerializer.SerializeAsync(compressionStream, content, jsonOptions);
            }

            return memoryStream.ToArray();

        }

        internal static ServiceBusMessage SetEnqueueTime(ServiceBusMessage message, TimeSpan? delay = null)
        {
            if (delay != null) message.ScheduledEnqueueTime = DateTimeOffset.UtcNow + delay.Value;
            return message;
        }

        public async Task SendMessage<T>(T content, string connectionString, string topicName, TimeSpan? delay = null)
        {
            var data = await Compress(content);
            var brokeredMessage = SetEnqueueTime(new ServiceBusMessage(data), delay);
            await using var client = new ServiceBusClient(connectionString);
            var sender = client.CreateSender(topicName);
            await sender.SendMessageAsync(brokeredMessage);
        }

        public async Task<T?> DecodeMessage<T>(byte[] content)
        {
            using var destinationStream = new MemoryStream();
            using (var sourceStream = new MemoryStream(content))
            using (var compressionStream = new GZipStream(sourceStream, CompressionMode.Decompress))
            {
                await compressionStream.CopyToAsync(destinationStream);
            }

            destinationStream.Seek(0, SeekOrigin.Begin);
            return await JsonSerializer.DeserializeAsync<T>(destinationStream, jsonOptions);
        }
    }
}
