using CyborgianStates.CommandHandling;
using CyborgianStates.Commands;
using CyborgianStates.Enums;
using CyborgianStates.Interfaces;
using CyborgianStates.MessageHandling;
using CyborgianStates.Services;
using CyborgianStates.Tests.CommandTests;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using NationStatesSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace CyborgianStates.Tests.Services
{
    public class BotServiceTests
    {
        private Mock<IMessageChannel> msgChannelMock;
        private Mock<IMessageHandler> msgHandlerMock;
        private Mock<IRequestDispatcher> requestDispatcherMock;
        private Mock<IOptions<AppSettings>> appSettingsMock;
        private Mock<IBackgroundServiceRegistry> backgroundServiceRegistryMock;
        private Mock<IDumpRetrievalService> dumpRetrievalServiceMock;
        private Mock<IDumpDataService> dumpDataServiceMock;

        public BotServiceTests()
        {
            msgHandlerMock = new Mock<IMessageHandler>(MockBehavior.Strict);
            msgHandlerMock.Setup(m => m.InitAsync()).Returns(Task.CompletedTask);
            msgHandlerMock.Setup(m => m.RunAsync()).Returns(Task.CompletedTask);
            msgHandlerMock.Setup(m => m.ShutdownAsync()).Returns(Task.CompletedTask);

            requestDispatcherMock = new Mock<IRequestDispatcher>(MockBehavior.Strict);
            requestDispatcherMock.Setup(r => r.Start());
            requestDispatcherMock.Setup(r => r.Shutdown());

            msgChannelMock = new Mock<IMessageChannel>(MockBehavior.Strict);
            appSettingsMock = new Mock<IOptions<AppSettings>>(MockBehavior.Strict);
            appSettingsMock
                .Setup(m => m.Value)
                .Returns(new AppSettings() { });
            backgroundServiceRegistryMock = new Mock<IBackgroundServiceRegistry>(MockBehavior.Strict);
            backgroundServiceRegistryMock.Setup(m => m.Register(It.IsAny<IBackgroundService>()));
            backgroundServiceRegistryMock.Setup(m => m.StartAsync()).Returns(Task.CompletedTask);
            backgroundServiceRegistryMock.Setup(m => m.ShutdownAsync()).Returns(Task.CompletedTask);
            dumpRetrievalServiceMock = new Mock<IDumpRetrievalService>(MockBehavior.Strict);
            dumpDataServiceMock = new Mock<IDumpDataService>(MockBehavior.Strict);
            ConfigureServices();
        }

        private IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IMessageHandler>(msgHandlerMock.Object);
            services.AddSingleton<IRequestDispatcher>(requestDispatcherMock.Object);
            services.AddSingleton<IResponseBuilder, ConsoleResponseBuilder>();
            services.AddSingleton<IOptions<AppSettings>>(appSettingsMock.Object);
            services.AddSingleton<IBackgroundServiceRegistry>(backgroundServiceRegistryMock.Object);
            services.AddSingleton<IDumpRetrievalService>(dumpRetrievalServiceMock.Object);
            services.AddSingleton<IDumpDataService>(dumpDataServiceMock.Object);
            services.AddSingleton<DumpRetrievalBackgroundService>(sp => new DumpRetrievalBackgroundService(sp));
            return services.BuildServiceProvider(new ServiceProviderOptions() { ValidateOnBuild = true });
        }

        [Fact]
        public async Task TestInitRunAndShutDownBotService()
        {
            var botService = new BotService(ConfigureServices());
            await botService.InitAsync().ConfigureAwait(false);
            await botService.RunAsync().ConfigureAwait(false);
            botService.IsRunning.Should().BeTrue();
            await botService.ShutdownAsync().ConfigureAwait(false);
            botService.IsRunning.Should().BeFalse();

            var sp = Program.ServiceProvider;
            Program.ServiceProvider = ConfigureServices();
            _ = new BotService();
            Program.ServiceProvider = sp;
        }

        [Fact]
        public async Task TestIsRelevant()
        {
            msgChannelMock.Setup(m => m.WriteToAsync(It.IsAny<CommandResponse>())).Returns(Task.CompletedTask);

            var slashCommand = BaseCommandTests.GetSlashCommand(new());
            Message message = new Message(0, "test", msgChannelMock.Object, slashCommand.Object);
            var botService = new BotService(ConfigureServices());
            await botService.InitAsync().ConfigureAwait(false);
            await botService.RunAsync().ConfigureAwait(false);

            MessageReceivedEventArgs eventArgs = new MessageReceivedEventArgs(null);
            msgHandlerMock.Raise(m => m.MessageReceived += null, this, eventArgs);

            eventArgs = new MessageReceivedEventArgs(message);
            msgHandlerMock.Raise(m => m.MessageReceived += null, this, eventArgs);

            AppSettings.IsTesting = true;
            AppSettings.Configuration = "test";
            eventArgs = new MessageReceivedEventArgs(message);
            msgHandlerMock.Raise(m => m.MessageReceived += null, this, eventArgs);
            AppSettings.IsTesting = false;

            message = new Message(1, "test", msgChannelMock.Object);
            eventArgs = new MessageReceivedEventArgs(message);
            msgHandlerMock.Raise(m => m.MessageReceived += null, this, eventArgs);

            message = new Message(1, "test", msgChannelMock.Object, slashCommand.Object);
            eventArgs = new MessageReceivedEventArgs(message);
            msgHandlerMock.Raise(m => m.MessageReceived += null, this, eventArgs);
        }

        [Fact]
        public async Task TestStartupProgressMessageAndShutDown()
        {
            Program.ServiceProvider = Program.ConfigureServices();

            CommandHandler.Clear();
            CommandHandler.Register(new CommandDefinition(typeof(PingCommand), new List<string>() { "ping" }));

            CommandHandler.Count.Should().Be(1);

            var botService = new BotService(ConfigureServices());
            await botService.InitAsync().ConfigureAwait(false);
            await botService.RunAsync().ConfigureAwait(false);

            botService.IsRunning.Should().BeTrue();
            msgHandlerMock.Verify(m => m.InitAsync(), Times.Once);
            msgHandlerMock.Verify(m => m.RunAsync(), Times.Once);

            CommandResponse commandResponse = new CommandResponse(CommandStatus.Error, "");

            msgChannelMock.Setup(m => m.ReplyToAsync(It.IsAny<Message>(), It.IsAny<CommandResponse>(), It.IsAny<bool>()))
                .Callback<Message, CommandResponse, bool>((m, cr, b) =>
                 {
                     commandResponse = cr;
                 })
                .Returns(Task.CompletedTask);

            Message message = new Message(0, "ping", msgChannelMock.Object);
            MessageReceivedEventArgs eventArgs = new MessageReceivedEventArgs(message);
            msgHandlerMock.Raise(m => m.MessageReceived += null, this, eventArgs);
            commandResponse.Status.Should().Be(CommandStatus.Success);
            commandResponse.Content.Should().Be("Pong !");

            await Task.Delay(1000).ConfigureAwait(false);

            await botService.ShutdownAsync().ConfigureAwait(false);

            botService.IsRunning.Should().BeFalse();
            msgHandlerMock.Verify(m => m.ShutdownAsync(), Times.Once);
        }
    }
}