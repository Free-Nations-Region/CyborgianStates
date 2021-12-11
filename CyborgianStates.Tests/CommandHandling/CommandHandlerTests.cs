using CyborgianStates.CommandHandling;
using CyborgianStates.Commands;
using CyborgianStates.Interfaces;
using CyborgianStates.MessageHandling;
using Discord;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace CyborgianStates.Tests.CommandHandling
{
    public class CommandHandlerTests
    {
        [Fact]
        public async Task TestExecutePingCommand()
        {
            CommandHandler.Clear();
            CommandHandler.Register(new CommandDefinition(typeof(PingCommand), new List<string>() { "ping" })
            {
                IsSlashCommand = true,
                Name = "ping",
                Description = "Pings",
                SlashCommandParameters = new List<SlashCommandParameter>
                {
                    new SlashCommandParameter(){ Name = "name", Type = ApplicationCommandOptionType.String, IsRequired = false, Description = "The region name" }
                }
            });
            var def = CommandHandler.Definitions[0];
            def.Name.Should().Be("ping");
            def.Description.Should().Be("Pings");
            def.IsSlashCommand.Should().BeTrue();
            def.SlashCommandParameters.Count.Should().Be(1);
            var defParam = def.SlashCommandParameters[0];
            defParam.Name.Should().Be("name");
            defParam.Description.Should().Be("The region name");
            defParam.IsRequired.Should().BeFalse();
            defParam.Type.Should().Be(ApplicationCommandOptionType.String);
            var result = await CommandHandler.ExecuteAsync(new Message(0, "ping ", new ConsoleMessageChannel())).ConfigureAwait(false);
            result.Should().BeOfType<CommandResponse>();
            result.Status.Should().Be(CommandStatus.Success);
        }

        [Fact]
        public void TestExecuteWithEmptyMessage()
        {
            Assert.ThrowsAsync<ArgumentNullException>(() => CommandHandler.ExecuteAsync(null));
        }

        [Fact]
        public async Task TestExecuteWithUnresolveableMessage()
        {
            CommandHandler.Clear();
            var result = await CommandHandler.ExecuteAsync(new Message(0, "unknown ", new ConsoleMessageChannel())).ConfigureAwait(false);
            result.Should().BeNull();
        }

        [Fact]
        public void TestRegisterCommand()
        {
            CommandHandler.Clear();
            CommandHandler.Register(new CommandDefinition(typeof(PingCommand), new List<string>() { "ping" }));
            CommandHandler.Count.Should().Be(1);
            CommandHandler.Definitions.Count.Should().Be(1);
        }

        [Fact]
        public void TestRegisterCommandWithInvalidCommandDefinition()
        {
            Assert.Throws<ArgumentNullException>(() => CommandHandler.Register(null));
            Assert.Throws<InvalidOperationException>(() => CommandHandler.Register(new CommandDefinition(typeof(ICommand), new List<string>())));
            Assert.Throws<InvalidOperationException>(() => CommandHandler.Register(new CommandDefinition(null, new List<string>() { "ping" })));
            Assert.Throws<InvalidOperationException>(() => CommandHandler.Register(new CommandDefinition(typeof(string), new List<string>() { "ping" })));
        }

        [Fact]
        public async Task TestResolvePingCommand()
        {
            CommandHandler.Clear();
            CommandHandler.Register(new CommandDefinition(typeof(PingCommand), new List<string>() { "ping" }));
            var resolved = await CommandHandler.ResolveAsync("ping").ConfigureAwait(false);
            resolved.Should().BeOfType<PingCommand>();
        }

        [Fact]
        public async Task TestResolveUnknownCommand()
        {
            var result = await CommandHandler.ResolveAsync("unknownCommand").ConfigureAwait(false);
            result.Should().BeNull();
        }

        [Fact]
        public void TestResolveWithEmptyTrigger()
        {
            Assert.ThrowsAsync<ArgumentNullException>(() => CommandHandler.ResolveAsync(null));
            Assert.ThrowsAsync<ArgumentNullException>(() => CommandHandler.ResolveAsync(string.Empty));
            Assert.ThrowsAsync<ArgumentNullException>(() => CommandHandler.ResolveAsync("    "));
        }
    }
}