using CyborgianStates.CommandHandling;
using CyborgianStates.Commands;
using CyborgianStates.Interfaces;
using CyborgianStates.MessageHandling;
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
    public class AboutCommandTests
    {
        [Fact]
        public async Task TestExecuteSuccess()
        {
            var message = new Message(0, "about", new ConsoleMessageChannel());

            var command = new AboutCommand(ConfigureServices());
            command.SetCancellationToken(CancellationToken.None);
            var response = await command.Execute(message);
            response.Status.Should().Be(CommandStatus.Success);
            response.Content.Should().Contain("About CyborgianStates");
            response.Content.Should().Contain("Developed by Drehtisch");
            response.Content.Should().Contain($"Github{Environment.NewLine}[CyborgianStates]");
            response.Content.Should().Contain($"Support{Environment.NewLine}via [OpenCollective]");
        }

        private IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            var options = new Mock<IOptions<AppSettings>>(MockBehavior.Strict);
            options.SetupGet(m => m.Value).Returns(new AppSettings() { SeperatorChar = '$', Contact = "contact@example.com" });
            services.AddSingleton<IResponseBuilder, ConsoleResponseBuilder>();
            services.AddSingleton(typeof(IOptions<AppSettings>), options.Object);
            return services.BuildServiceProvider(new ServiceProviderOptions() { ValidateOnBuild = true });
        }
    }
}