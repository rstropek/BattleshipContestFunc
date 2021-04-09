using Azure.Messaging.ServiceBus;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace BattleshipContestFunc.Tests
{
    public class MessageSenderTests
    {
        [Fact]
        public async Task CompressDecompress()
        {
            const string sentence = "The quick brown fox jumps over the lazy dog";

            var ms = new MessageSender(new JsonSerializerOptions());
            var buffer = await ms.Compress(sentence);
            var text = await ms.DecodeMessage<string>(buffer);

            Assert.Equal(sentence, text);
        }

        [Fact]
        public void SetEnqueueTime()
        {
            var sbm = new ServiceBusMessage();
            sbm = MessageSender.SetEnqueueTime(sbm, TimeSpan.FromMinutes(1));

            Assert.True(sbm.ScheduledEnqueueTime > DateTimeOffset.UtcNow.AddSeconds(30));
        }

        [Fact]
        public void SetEnqueueTimeEmpty()
        {
            var sbm = new ServiceBusMessage();
            sbm = MessageSender.SetEnqueueTime(sbm);

            Assert.True(sbm.ScheduledEnqueueTime <= DateTimeOffset.UtcNow);
        }
    }
}
