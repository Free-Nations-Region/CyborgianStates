using CyborgianStates.MessageHandling;
using CyborgianStates.Tests.CommandTests;
using CyborgianStates.Wrapper;
using DataAbstractions.Dapper;
using Discord;
using Discord.WebSocket;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CyborgianStates.Tests.MessageHandling
{
    public class DiscordMessageHandlerTests
    {

        private Mock<IOptions<AppSettings>> appSettingsMock;
        private Mock<DiscordClientWrapper> clientMock;
        private Mock<IDataAccessor> dataAccessorMock;
        public DiscordMessageHandlerTests()
        {
            dataAccessorMock = new Mock<IDataAccessor>(MockBehavior.Strict);
            appSettingsMock = new Mock<IOptions<AppSettings>>(MockBehavior.Strict);
            appSettingsMock
                .Setup(m => m.Value)
                .Returns(new AppSettings() { SeperatorChar = '$', DiscordBotLoginToken = string.Empty });
            var mockClient = new Mock<DiscordClientWrapper>(MockBehavior.Strict);
            mockClient.Setup(m => m.LoginAsync(It.IsAny<Discord.TokenType>(), "", true))
                .Returns(Task.CompletedTask);
            mockClient.Setup(m => m.StartAsync()).Returns(Task.CompletedTask);
            mockClient.Setup(m => m.LogoutAsync()).Returns(Task.CompletedTask);
            mockClient.Setup(m => m.StopAsync()).Returns(Task.CompletedTask);
            mockClient.Setup(m => m.Dispose());
            mockClient.SetupGet<bool>(m => m.IsTest).Returns(true);
            clientMock = mockClient;

        }
        [Fact]
        public async Task TestSimpleEventLogMethods()
        {

            var handler = new DiscordMessageHandler(appSettingsMock.Object, clientMock.Object, dataAccessorMock.Object);
            await handler.InitAsync();
#pragma warning disable CS4014 // Da auf diesen Aufruf nicht gewartet wird, wird die Ausführung der aktuellen Methode vor Abschluss des Aufrufs fortgesetzt.
            handler.RunAsync();
#pragma warning restore CS4014 // Da auf diesen Aufruf nicht gewartet wird, wird die Ausführung der aktuellen Methode vor Abschluss des Aufrufs fortgesetzt.
            handler.IsRunning.Should().BeTrue();
            Assert.Throws<ArgumentNullException>(() => { new DiscordMessageHandler(null, clientMock.Object, dataAccessorMock.Object); });
            Assert.Throws<ArgumentNullException>(() => { new DiscordMessageHandler(appSettingsMock.Object, null, dataAccessorMock.Object); });
            Assert.Throws<ArgumentNullException>(() => { new DiscordMessageHandler(appSettingsMock.Object, clientMock.Object, null); });
            clientMock.Raise(m => m.Ready += null);
            clientMock.Raise(m => m.Connected += null);
            clientMock.Raise(m => m.LoggedIn += null);
            clientMock.Raise(m => m.Disconnected += null, new Exception());
            clientMock.Raise(m => m.LoggedOut += null);
            await handler.ShutdownAsync();
            handler.IsRunning.Should().BeFalse();
        }

        [Fact]
        public async Task TestDiscordLogMethod()
        {
            var handler = new DiscordMessageHandler(appSettingsMock.Object, clientMock.Object, dataAccessorMock.Object);
            await handler.InitAsync();
            clientMock.Raise(m => m.Log += null, new LogMessage(LogSeverity.Info, "", "Test"));
            clientMock.Raise(m => m.Log += null, new LogMessage(LogSeverity.Warning, "", "Test"));
            clientMock.Raise(m => m.Log += null, new LogMessage(LogSeverity.Error, "", "Test"));
            clientMock.Raise(m => m.Log += null, new LogMessage(LogSeverity.Critical, "", "Test"));
            clientMock.Raise(m => m.Log += null, new LogMessage(LogSeverity.Debug, "", "Test"));
        }

        [Fact]
        public async Task TestDiscordMessageReceived()
        {
            var handler = new DiscordMessageHandler(appSettingsMock.Object, clientMock.Object, dataAccessorMock.Object);
            await handler.InitAsync();
            var mockMessage = new Mock<IMessage>();
            var mockUser = new Mock<IUser>();
            mockUser.SetupGet<ulong>(m => m.Id).Returns(0);
            mockMessage.SetupGet<IUser>(m => m.Author).Returns(mockUser.Object);
            mockMessage.SetupGet<string>(m => m.Content).Returns("test");
#pragma warning disable CS4014 // Da auf diesen Aufruf nicht gewartet wird, wird die Ausführung der aktuellen Methode vor Abschluss des Aufrufs fortgesetzt.
            handler.HandleMessageAsync(mockMessage.Object);
            mockMessage.SetupGet<string>(m => m.Content).Returns("$test");
            handler.HandleMessageAsync(mockMessage.Object);
#pragma warning restore CS4014 // Da auf diesen Aufruf nicht gewartet wird, wird die Ausführung der aktuellen Methode vor Abschluss des Aufrufs fortgesetzt.
        }

        [Fact]
        public async Task TestDiscordSlashCommandReceived()
        {
            var handler = new DiscordMessageHandler(appSettingsMock.Object, clientMock.Object, dataAccessorMock.Object);
            await handler.InitAsync();
            var mockUser = new Mock<IUser>();
            mockUser.SetupGet<ulong>(m => m.Id).Returns(0);
            mockUser.SetupGet<string>(m => m.Username).Returns("test");

            var command = BaseCommandTests.GetSlashCommand(new(), "test", mockUser.Object);
#pragma warning disable CS4014 // da auf diesen aufruf nicht gewartet wird, wird die ausführung der aktuellen methode vor abschluss des aufrufs fortgesetzt.
            handler.HandleSlashCommandAsync(command.Object);
#pragma warning restore CS4014 // da auf diesen aufruf nicht gewartet wird, wird die ausführung der aktuellen methode vor abschluss des aufrufs fortgesetzt.
        }
    }
}
