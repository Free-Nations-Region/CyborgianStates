using CyborgianStates.CommandHandling;
using CyborgianStates.MessageHandling;
using System;
using System.Threading.Tasks;
using Xunit;

namespace CyborgianStates.Tests.MessageHandling
{
    public class ConsoleMessageChannelTests
    {
        [Fact]
        public async Task WriteToTest()
        {
            var channel = new ConsoleMessageChannel();
            await channel.WriteToAsync("Test").ConfigureAwait(false);
            await channel.WriteToAsync(new CommandResponse(CommandStatus.Success, "Test")).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await channel.WriteToAsync(string.Empty).ConfigureAwait(false)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await channel.WriteToAsync((string) null).ConfigureAwait(false)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await channel.WriteToAsync(" ").ConfigureAwait(false)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await channel.WriteToAsync((CommandResponse) null).ConfigureAwait(false)).ConfigureAwait(false);
        }
        
        [Fact]
        public async Task ReplyToTest()
        {
            var channel = new ConsoleMessageChannel();
            var message = new Message(0, "Test", new ConsoleMessageChannel());
            var commandResponse = new CommandResponse(CommandStatus.Success, "Test");
            await channel.ReplyToAsync(message, commandResponse.Content).ConfigureAwait(false);
            await channel.ReplyToAsync(message, commandResponse).ConfigureAwait(false);
            await channel.ReplyToAsync(message, commandResponse.Content, true).ConfigureAwait(false);
            await channel.ReplyToAsync(message, commandResponse, true).ConfigureAwait(false);
            await channel.ReplyToAsync(message, "Test").ConfigureAwait(false);
            await channel.ReplyToAsync(message, "Test", true).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await channel.ReplyToAsync(null, new CommandResponse(CommandStatus.Success, "Test")).ConfigureAwait(false)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await channel.ReplyToAsync(message, (CommandResponse)null).ConfigureAwait(false)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await channel.ReplyToAsync(message, string.Empty).ConfigureAwait(false)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await channel.ReplyToAsync(null, "Test").ConfigureAwait(false)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await channel.ReplyToAsync(message, string.Empty, true).ConfigureAwait(false)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await channel.ReplyToAsync(null, "Test", true).ConfigureAwait(false)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await channel.ReplyToAsync(null, new CommandResponse(CommandStatus.Success, "Test"),true).ConfigureAwait(false)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await channel.ReplyToAsync(message, (CommandResponse) null, true).ConfigureAwait(false)).ConfigureAwait(false);
        }
    }
}