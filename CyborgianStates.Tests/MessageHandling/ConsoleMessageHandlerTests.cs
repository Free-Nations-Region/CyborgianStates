using CyborgianStates.Interfaces;
using CyborgianStates.MessageHandling;
using FluentAssertions;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace CyborgianStates.Tests.MessageHandling
{
    public class ConsoleMessageHandlerTests
    {
        [Fact]
        public async Task ConsoleMessageHandlerTest()
        {
            var mockInput = new Mock<IUserInput>(MockBehavior.Strict);
            mockInput.Setup(m => m.GetInput()).Returns("ping");
            IUserInput userInput = mockInput.Object;
            ConsoleMessageHandler consoleMessageHandler = new ConsoleMessageHandler(userInput);
            consoleMessageHandler.MessageReceived += ConsoleMessageHandler_MessageReceived;
            await consoleMessageHandler.InitAsync().ConfigureAwait(false);
            var task = Task.Run(async () => await consoleMessageHandler.RunAsync().ConfigureAwait(false));
            await Task.Delay(50).ConfigureAwait(false);
            consoleMessageHandler.IsRunning.Should().BeTrue();
            mockInput.Verify(m => m.GetInput(), Times.AtLeastOnce);
            await consoleMessageHandler.ShutdownAsync().ConfigureAwait(false);
            task.Wait();
            consoleMessageHandler.IsRunning.Should().BeFalse();
        }

        [Fact]
        public async Task ConsoleMessageHandlerTestWithEmptyMessage()
        {
            var mockInput = new Mock<IUserInput>(MockBehavior.Strict);
            mockInput.Setup(m => m.GetInput()).Returns(string.Empty);
            IUserInput userInput = mockInput.Object;
            ConsoleMessageHandler consoleMessageHandler = new ConsoleMessageHandler(userInput);
            await consoleMessageHandler.InitAsync().ConfigureAwait(false);
            var task = Task.Run(async () => await consoleMessageHandler.RunAsync().ConfigureAwait(false));
            await Task.Delay(1000).ConfigureAwait(false);
            consoleMessageHandler.IsRunning.Should().BeTrue();
            mockInput.Verify(m => m.GetInput(), Times.AtLeastOnce);
            await consoleMessageHandler.ShutdownAsync().ConfigureAwait(false);
            task.Wait();
            consoleMessageHandler.IsRunning.Should().BeFalse();
        }

        private void ConsoleMessageHandler_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Console.WriteLine($"MessageReceived -> AuthorId: {e.Message.AuthorId} Channel: {e.Message.Channel.GetType().Name} Content: {e.Message.Content}");
        }
    }
}