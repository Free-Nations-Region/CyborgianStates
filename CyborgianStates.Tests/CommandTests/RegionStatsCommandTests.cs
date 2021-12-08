using CyborgianStates.CommandHandling;
using CyborgianStates.Commands;
using CyborgianStates.Data.Models.Dump;
using CyborgianStates.Interfaces;
using CyborgianStates.MessageHandling;
using CyborgianStates.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NationStatesSharp.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;

namespace CyborgianStates.Tests.CommandTests
{
    public class RegionStatsCommandTests : BaseCommandTests
    {
        private IServiceProvider _serviceProvider;
        private Mock<IDumpDataService> _dumpDataService;
        public RegionStatsCommandTests()
        {
            _dumpDataService = new Mock<IDumpDataService>(MockBehavior.Strict);
            _serviceProvider = ConfigureServices().BuildServiceProvider();
        }

        protected override ServiceCollection ConfigureServices()
        {
            var collection = base.ConfigureServices();
            collection.AddSingleton<IDumpDataService>(_dumpDataService.Object);
            return collection;
        }

        [Fact]
        public async override Task TestCancel()
        {
            _dumpDataService.SetupGet(m => m.Status).Returns(Enums.DumpDataStatus.Ready);
            var collection = ConfigureServices();
            _serviceProvider = collection.BuildServiceProvider();
            var message = new Message(0, "region The Free Nations Region", new ConsoleMessageChannel());
            var command = new RegionStatsCommand(_serviceProvider);
            var source = new CancellationTokenSource();
            source.CancelAfter(250);
            command.SetCancellationToken(source.Token);
            TestRequestDispatcher.PrepareNextRequest(RequestStatus.Canceled);
            var response = await command.Execute(message);
            response.Status.Should().Be(CommandStatus.Error);
            response.Content.Should().StartWith($"Something went wrong{Environment.NewLine}{Environment.NewLine}Request/Command has been canceled. Sorry :(");
        }
        [Fact]
        public async override Task TestExecuteSuccess()
        {
            _dumpDataService.SetupGet(m => m.Status).Returns(Enums.DumpDataStatus.Ready);
            _dumpDataService.Setup(m => m.GetRegionByName(It.IsAny<string>())).Returns(new Data.Models.Dump.DumpRegion() { Delegate = "greenerica", Founder = "Testlandia" });
            _dumpDataService.Setup(m => m.GetNationByName(It.Is<string>(m => m == "greenerica"))).Returns(new Data.Models.Dump.DumpNation() { UnescapedName = "Greenerica", Endorsements = new List<string>() });
            _dumpDataService.Setup(m => m.GetNationByName(It.Is<string>(m => m.ToLower() == "testlandia"))).Returns(new Data.Models.Dump.DumpNation() { UnescapedName = "Testlandia" });
            _dumpDataService.Setup(m => m.GetNationByName(It.Is<string>(m => m.ToLower() == "olvaria"))).Returns(new Data.Models.Dump.DumpNation() { UnescapedName = "Olvaria" });
            _dumpDataService.Setup(m => m.GetWANationsByRegionName(It.IsAny<string>())).Returns(Enumerable.Repeat(new DumpNation() { }, 4).ToList());
            _dumpDataService.Setup(m => m.GetEndoSumByRegionName(It.IsAny<string>())).Returns(4);
            var collection = ConfigureServices();
            _serviceProvider = collection.BuildServiceProvider();
            var message = new Message(0, "region Testregionia", new ConsoleMessageChannel());
            var command = new RegionStatsCommand(_serviceProvider);
            var nstatsXmlResult = XDocument.Load(Path.Combine("TestData", "testregionia-region-stats.xml"));
            TestRequestDispatcher.PrepareNextRequest(response: nstatsXmlResult);
            var rofficersXmlResult = XDocument.Load(Path.Combine("TestData", "testregionia-officers.xml"));
            TestRequestDispatcher.PrepareNextRequest(response: rofficersXmlResult);
            var response = await command.Execute(message);
            response.Status.Should().Be(CommandStatus.Success);
        }
        [Fact]
        public override Task TestExecuteWithErrors()
        {
            return Task.CompletedTask;
        }
    }
}