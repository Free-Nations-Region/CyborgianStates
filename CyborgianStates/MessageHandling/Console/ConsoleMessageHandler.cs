using CyborgianStates.Interfaces;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Threading.Tasks;
using ILogger = Serilog.ILogger;

namespace CyborgianStates.MessageHandling
{
    public class ConsoleMessageHandler : IMessageHandler
    {
        private readonly IUserInput _input;
        private readonly ILogger _logger;

        public ConsoleMessageHandler(IUserInput input)
        {
            _input = input;
            _logger = Log.ForContext<ConsoleMessageHandler>();
        }

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public bool IsRunning { get; private set; }

        public Task InitAsync()
        {
            _logger.Information("-- ConsoleMessageHandler Init --");
            return Task.CompletedTask;
        }

        public async Task RunAsync()
        {
            IsRunning = true;
            while (IsRunning)
            {
                var input = _input.GetInput();
                if (!string.IsNullOrWhiteSpace(input))
                {
                    MessageReceived?.Invoke(this, new MessageReceivedEventArgs(new Message(0, input, new ConsoleMessageChannel())));
                }
                await Task.Delay(50).ConfigureAwait(false);
            }
        }

        public Task ShutdownAsync()
        {
            IsRunning = false;

            return Task.CompletedTask;
        }
    }
}