using CyborgianStates.CommandHandling;
using CyborgianStates.Commands;
using CyborgianStates.Interfaces;
using CyborgianStates.MessageHandling;
using CyborgianStates.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CyborgianStates.Tests.CommandTests
{
    public class AboutCommandTests : BaseCommandTests
    {
        private IServiceProvider _serviceProvider;
        public AboutCommandTests()
        {
            _serviceProvider = ConfigureServices(new TestRequestDispatcher()).BuildServiceProvider();
        }

        [Fact]
        public override async Task TestExecuteSuccess()
        {
            var sp = Program.ServiceProvider;
            Program.ServiceProvider = _serviceProvider;
            _ = new AboutCommand();
            Program.ServiceProvider = sp;

            var message = new Message(0, "about", new ConsoleMessageChannel());

            var command = new AboutCommand(_serviceProvider);
            command.SetCancellationToken(CancellationToken.None);
            var response = await command.Execute(message);
            response.Status.Should().Be(CommandStatus.Success);
            response.Content.Should().Contain("About CyborgianStates");
            response.Content.Should().Contain("Developed by Drehtisch");
            response.Content.Should().Contain($"Github{Environment.NewLine}[CyborgianStates]");
            response.Content.Should().Contain($"Support{Environment.NewLine}via [OpenCollective]");
            
        }
        public override Task TestCancel()
        {
            return Task.CompletedTask;
        }

        public override Task TestExecuteWithErrors()
        {
            return Task.CompletedTask;
        }
    }
}