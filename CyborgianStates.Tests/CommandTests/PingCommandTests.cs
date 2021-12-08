using CyborgianStates.CommandHandling;
using CyborgianStates.Commands;
using CyborgianStates.MessageHandling;
using FluentAssertions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CyborgianStates.Tests.CommandTests
{
    public class PingCommandTests
    {
        [Fact]
        public async Task TestPingCommand()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await new PingCommand().Execute(null).ConfigureAwait(false)).ConfigureAwait(false);
            var result = await new PingCommand().Execute(new Message(0, "ping", new ConsoleMessageChannel())).ConfigureAwait(false);
            result.Status.Should().Be(CommandStatus.Success);
            result.Content.Should().Be("Pong !");
        }
    }
}