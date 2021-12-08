using CyborgianStates.CommandHandling;
using CyborgianStates.Commands;
using CyborgianStates.MessageHandling;
using CyborgianStates.Tests.CommandHandling;
using CyborgianStates.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NationStatesSharp;
using NationStatesSharp.Enums;
using NationStatesSharp.Interfaces;
using System;
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
        public NationStatsCommandTests()
        {
            _serviceProvider = ConfigureServices().BuildServiceProvider();
        }
        [Fact]
        public async Task TestEmptyExecute()
        {
            
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await new NationStatsCommand(_serviceProvider).Execute(null).ConfigureAwait(false)).ConfigureAwait(false);
            var message = new Message(0, "nation", new ConsoleMessageChannel());
            var response = await new NationStatsCommand(_serviceProvider).Execute(message);
            response.Status.Should().Be(CommandStatus.Error);
            response.Content.Should().StartWith($"Something went wrong{Environment.NewLine}{Environment.NewLine}No parameter passed.");
        }

        [Fact]
        public override async Task TestCancel()
        {
            var message = new Message(0, "nation Testlandia", new ConsoleMessageChannel());
            var command = new NationStatsCommand(_serviceProvider);
            var source = new CancellationTokenSource();
            source.CancelAfter(250);
            command.SetCancellationToken(source.Token);
            TestRequestDispatcher.PrepareNextRequest(RequestStatus.Canceled);
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
            TestRequestDispatcher.PrepareNextRequest(RequestStatus.Failed, exception: new HttpRequestFailedException());
            var response = await command.Execute(message);
            response.Status.Should().Be(CommandStatus.Error);
            response.Content.Should().StartWith($"Something went wrong{Environment.NewLine}{Environment.NewLine}Exception of type 'NationStatesSharp.HttpRequestFailedException' was thrown.");
            // InvalidOperation
            response = await command.Execute(message);
            response.Status.Should().Be(CommandStatus.Error);
            response.Content.Should().StartWith($"Something went wrong{Environment.NewLine}{Environment.NewLine}An unexpected error occured. Please contact the bot administrator.");
            // NationStats Response not XmlDocument
            TestRequestDispatcher.PrepareNextRequest();
            response = await command.Execute(message);
            response.Status.Should().Be(CommandStatus.Error);
            response.Content.Should().StartWith($"Something went wrong{Environment.NewLine}{Environment.NewLine}An unexpected error occured. Please contact the bot administrator.");
            // RegionalOfficer Response not XmlDocument
            var nstatsXmlResult = XDocument.Load(Path.Combine("TestData", "testlandia-nation-stats.xml"));
            TestRequestDispatcher.PrepareNextRequest(response: nstatsXmlResult);
            TestRequestDispatcher.PrepareNextRequest();
            response = await command.Execute(message);
            response.Status.Should().Be(CommandStatus.Error);
            response.Content.Should().StartWith($"Something went wrong{Environment.NewLine}{Environment.NewLine}An unexpected error occured. Please contact the bot administrator.");
            // Empty RegionalOfficer Xml
            TestRequestDispatcher.PrepareNextRequest(response: nstatsXmlResult);
            var roResult = XDocument.Parse("<xml></xml>");
            TestRequestDispatcher.PrepareNextRequest(response: roResult);
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
            TestRequestDispatcher.PrepareNextRequest(response: nstatsXmlResult);
            var rofficersXmlResult = XDocument.Load(Path.Combine("TestData", "testregionia-officers.xml"));
            TestRequestDispatcher.PrepareNextRequest(response: rofficersXmlResult);
            var response = await command.Execute(message);
            response.Status.Should().Be(CommandStatus.Success);
            var dateJoined = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(99.94290131428));
            response.Content.Should().StartWith($"The дСвобода Мысл of Testlandia{Environment.NewLine}{Environment.NewLine}38.068 billion Testlandians | Last active 7 hours ago{Environment.NewLine}{Environment.NewLine}Founded{Environment.NewLine}01.01.1970 (0){Environment.NewLine}{Environment.NewLine}Region                                                            Regional Officer                   {Environment.NewLine}[Testregionia](https://www.nationstates.net/region=testregionia)  &lt;h1&gt;Field Tester&lt;/h1&gt;  {Environment.NewLine}{Environment.NewLine}Resident Since{Environment.NewLine}{dateJoined:dd.MM.yyyy} (99 d){Environment.NewLine}{Environment.NewLine}New York Times Democracy{Environment.NewLine}C: Excellent (69.19) | E: Very Strong (80.00) | P: Superb (76.29){Environment.NewLine}{Environment.NewLine}WA Member{Environment.NewLine}0 endorsements | 6106.00 Influence (Eminence Grise){Environment.NewLine}{Environment.NewLine}WA Vote{Environment.NewLine}GA: UNDECIDED | SC: UNDECIDED{Environment.NewLine}{Environment.NewLine}Links{Environment.NewLine}[Dispatches](https://www.nationstates.net/page=dispatches/nation=testlandia)  |  [Cards Deck](https://www.nationstates.net/page=deck/nation=testlandia)  |  [Challenge](https://www.nationstates.net/page=challenge?entity_name=testlandia)");
        }
    }
}