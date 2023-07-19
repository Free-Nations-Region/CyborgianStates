using CyborgianStates.CommandHandling;
using CyborgianStates.Commands;
using CyborgianStates.Interfaces;
using CyborgianStates.MessageHandling;
using CyborgianStates.Tests.CommandHandling;
using CyborgianStates.Tests.Helpers;
using Discord;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using NationStatesSharp;
using NationStatesSharp.Enums;
using NationStatesSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace CyborgianStates.Tests.CommandTests
{
    public class NationStatsCommandTests : BaseCommandTests
    {
        private IServiceProvider _serviceProvider;
        private TestRequestDispatcher _requestDispatcher;
        public NationStatsCommandTests()
        {
            _requestDispatcher = new();
            _serviceProvider = ConfigureServices(_requestDispatcher).BuildServiceProvider();
        }
        [Fact]
        public async Task TestEmptyExecute()
        {
            var sp = Program.ServiceProvider;
            Program.ServiceProvider = _serviceProvider;
            _ = new NationStatsCommand();
            Program.ServiceProvider = sp;

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await new NationStatsCommand(_serviceProvider).Execute(null).ConfigureAwait(false)).ConfigureAwait(false);
            var message = new Message(0, "nation", new ConsoleMessageChannel());
            var response = await new NationStatsCommand(_serviceProvider).Execute(message);
            response.Status.Should().Be(CommandStatus.Error);
            response.Content.Should().StartWith($"That didn't work.{Environment.NewLine}{Environment.NewLine}No parameter passed.");
        }

        [Fact]
        public override async Task TestCancel()
        {
            var message = new Message(0, "nation Testlandia", new ConsoleMessageChannel());
            var command = new NationStatsCommand(_serviceProvider);
            _requestDispatcher.PrepareNextRequest(RequestStatus.Canceled);
            var source = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            command.SetCancellationToken(source.Token);
            //source.CancelAfter(250);
            var response = await command.Execute(message);
            response.Status.Should().Be(CommandStatus.Error);
            response.Content.Should().StartWith($"Something went wrong{Environment.NewLine}{Environment.NewLine}Request/Command has been canceled. Sorry :(");
        }

        [Fact]
        public override async Task TestExecuteWithErrors()
        {
            var message = new Message(0, "nation Testlandia", new ConsoleMessageChannel());
            var command = new NationStatsCommand(_serviceProvider);
            // HttpRequestFailed
            _requestDispatcher.PrepareNextRequest(RequestStatus.Failed, exception: new HttpRequestFailedException());
            var response = await command.Execute(message);
            response.Status.Should().Be(CommandStatus.Error);
            response.Content.Should().StartWith($"Something went wrong{Environment.NewLine}{Environment.NewLine}Exception of type 'NationStatesSharp.HttpRequestFailedException' was thrown.");
            // InvalidOperation
            response = await command.Execute(message);
            response.Status.Should().Be(CommandStatus.Error);
            response.Content.Should().StartWith($"Something went wrong{Environment.NewLine}{Environment.NewLine}An unexpected error occured. Please contact the bot administrator.");
            // NationStats Response not XmlDocument
            _requestDispatcher.PrepareNextRequest();
            response = await command.Execute(message);
            response.Status.Should().Be(CommandStatus.Error);
            response.Content.Should().StartWith($"Something went wrong{Environment.NewLine}{Environment.NewLine}An unexpected error occured. Please contact the bot administrator.");
            // RegionalOfficer Response not XmlDocument
            var nstatsXmlResult = XDocument.Load(Path.Combine("TestData", "testlandia-nation-stats.xml"));
            _requestDispatcher.PrepareNextRequest(response: nstatsXmlResult);
            _requestDispatcher.PrepareNextRequest();
            response = await command.Execute(message);
            response.Status.Should().Be(CommandStatus.Error);
            response.Content.Should().StartWith($"Something went wrong{Environment.NewLine}{Environment.NewLine}An unexpected error occured. Please contact the bot administrator.");
            // Empty RegionalOfficer Xml
            _requestDispatcher.PrepareNextRequest(response: nstatsXmlResult);
            var roResult = XDocument.Parse("<xml></xml>");
            _requestDispatcher.PrepareNextRequest(response: roResult);
            response = await command.Execute(message);
            response.Status.Should().Be(CommandStatus.Success);
            response.Content.Should().StartWith($"The дСвобода Мысл of Testlandia");
        }

        [Fact]
        public override async Task TestExecuteSuccess()
        {

            var message = new Message(0, "nation Testlandia", new ConsoleMessageChannel());
            var command = new NationStatsCommand(_serviceProvider);
            var nstatsXmlResult = XDocument.Load(Path.Combine("TestData", "testlandia-nation-stats.xml"));
            _requestDispatcher.PrepareNextRequest(response: nstatsXmlResult);
            var rofficersXmlResult = XDocument.Load(Path.Combine("TestData", "testregionia-officers.xml"));
            _requestDispatcher.PrepareNextRequest(response: rofficersXmlResult);
            var response = await command.Execute(message);
            response.Status.Should().Be(CommandStatus.Success);
            var dateJoined = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(99.94290131428));
            response.Content.Should().StartWith($"The дСвобода Мысл of Testlandia{Environment.NewLine}{Environment.NewLine}38.068 billion Testlandians | Last active 7 hours ago{Environment.NewLine}{Environment.NewLine}Founded{Environment.NewLine}01.01.1970 (0){Environment.NewLine}{Environment.NewLine}Region                                                            Regional Officer                   {Environment.NewLine}[Testregionia](https://www.nationstates.net/region=testregionia)  &lt;h1&gt;Field Tester&lt;/h1&gt;  {Environment.NewLine}{Environment.NewLine}Resident Since{Environment.NewLine}{dateJoined:dd.MM.yyyy} (99 d){Environment.NewLine}{Environment.NewLine}New York Times Democracy{Environment.NewLine}C: Excellent (69.19) | E: Very Strong (80.00) | P: Superb (76.29){Environment.NewLine}{Environment.NewLine}WA Member{Environment.NewLine}0 endorsements | 6106.00 Influence (Eminence Grise){Environment.NewLine}{Environment.NewLine}WA Vote{Environment.NewLine}GA: UNDECIDED | SC: UNDECIDED{Environment.NewLine}{Environment.NewLine}Links{Environment.NewLine}[Dispatches](https://www.nationstates.net/page=dispatches/nation=testlandia)  |  [Cards Deck](https://www.nationstates.net/page=deck/nation=testlandia)  |  [Challenge](https://www.nationstates.net/page=challenge?entity_name=testlandia)");
        }

        [Fact]
        public async Task TestExecuteWithSlashCommandSuccess()
        {
            Mock<ISlashCommand> mockCommand = GetSlashCommand(new Dictionary<string, object>() { ["name"] = "Testlandia" });

            var message = new Message(0, "nation Testlandia", new ConsoleMessageChannel(), mockCommand.Object);
            var command = new NationStatsCommand(_serviceProvider);
            var nstatsXmlResult = XDocument.Load(Path.Combine("TestData", "testlandia-nation-stats.xml"));
            _requestDispatcher.PrepareNextRequest(response: nstatsXmlResult);
            var rofficersXmlResult = XDocument.Load(Path.Combine("TestData", "testregionia-officers.xml"));
            _requestDispatcher.PrepareNextRequest(response: rofficersXmlResult);
            var response = await command.Execute(message);
            response.Status.Should().Be(CommandStatus.Success);
            var dateJoined = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(99.94290131428));
            response.Content.Should().StartWith($"The дСвобода Мысл of Testlandia{Environment.NewLine}{Environment.NewLine}38.068 billion Testlandians | Last active 7 hours ago{Environment.NewLine}{Environment.NewLine}Founded{Environment.NewLine}01.01.1970 (0){Environment.NewLine}{Environment.NewLine}Region                                                            Regional Officer                   {Environment.NewLine}[Testregionia](https://www.nationstates.net/region=testregionia)  &lt;h1&gt;Field Tester&lt;/h1&gt;  {Environment.NewLine}{Environment.NewLine}Resident Since{Environment.NewLine}{dateJoined:dd.MM.yyyy} (99 d){Environment.NewLine}{Environment.NewLine}New York Times Democracy{Environment.NewLine}C: Excellent (69.19) | E: Very Strong (80.00) | P: Superb (76.29){Environment.NewLine}{Environment.NewLine}WA Member{Environment.NewLine}0 endorsements | 6106.00 Influence (Eminence Grise){Environment.NewLine}{Environment.NewLine}WA Vote{Environment.NewLine}GA: UNDECIDED | SC: UNDECIDED{Environment.NewLine}{Environment.NewLine}Links{Environment.NewLine}[Dispatches](https://www.nationstates.net/page=dispatches/nation=testlandia)  |  [Cards Deck](https://www.nationstates.net/page=deck/nation=testlandia)  |  [Challenge](https://www.nationstates.net/page=challenge?entity_name=testlandia)");
        }

        
    }
}