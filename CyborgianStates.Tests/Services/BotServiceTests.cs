using CyborgianStates.CommandHandling;
using CyborgianStates.Commands;
using CyborgianStates.Enums;
using CyborgianStates.Interfaces;
using CyborgianStates.MessageHandling;
using CyborgianStates.Services;
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
        private Mock<IUserRepository> userRepositoryMock;
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

            userRepositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);

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
            services.AddSingleton<IUserRepository>(userRepositoryMock.Object);
            services.AddSingleton<IResponseBuilder, ConsoleResponseBuilder>();
            services.AddSingleton<IOptions<AppSettings>>(appSettingsMock.Object);
            services.AddSingleton<IBackgroundServiceRegistry>(backgroundServiceRegistryMock.Object);
            services.AddSingleton<IDumpRetrievalService>(dumpRetrievalServiceMock.Object);
            services.AddSingleton<IDumpDataService>(dumpDataServiceMock.Object);
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
        }

        [Fact]
        public async Task TestIsRelevant()
        {
            userRepositoryMock.Setup(u => u.IsUserInDbAsync(It.IsAny<ulong>())).Returns(() => Task.FromResult(false));
            userRepositoryMock.Setup(u => u.AddUserToDbAsync(It.IsAny<ulong>())).Returns(() => Task.CompletedTask);
            userRepositoryMock.Setup(u => u.IsAllowedAsync(It.IsAny<string>(), It.IsAny<ulong>())).Returns(() => Task.FromResult(true));
            msgChannelMock.Setup(m => m.WriteToAsync(It.IsAny<CommandResponse>())).Returns(Task.CompletedTask);

            Message message = new Message(0, "test", msgChannelMock.Object);
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
        }

        [Fact]
        public async Task TestStartupProgressMessageAndShutDown()
        {
            Program.ServiceProvider = Program.ConfigureServices();
            userRepositoryMock.Setup(u => u.IsUserInDbAsync(It.IsAny<ulong>())).Returns(() => Task.FromResult(true));
            userRepositoryMock.Setup(u => u.AddUserToDbAsync(It.IsAny<ulong>())).Returns(() => Task.CompletedTask);
            userRepositoryMock.Setup(u => u.IsAllowedAsync(It.IsAny<string>(), It.IsAny<ulong>())).Returns(() => Task.FromResult(true));

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