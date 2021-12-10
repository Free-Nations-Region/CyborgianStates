using System;
using System.Threading;
using System.Threading.Tasks;
using CyborgianStates.CommandHandling;
using CyborgianStates.Interfaces;
using CyborgianStates.MessageHandling;
using Microsoft.Extensions.Logging;
using Serilog;
using ILogger = Serilog.ILogger;

namespace CyborgianStates.Commands
{
    public class PingCommand : ICommand
    {
        private readonly ILogger _logger;
        private CancellationToken token;

        public PingCommand()
        {
            _logger = Log.ForContext<PingCommand>();
        }

        public Task<CommandResponse> Execute(Message message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            return Task.FromResult(new CommandResponse(CommandStatus.Success, "Pong !"));
        }

        public void SetCancellationToken(CancellationToken cancellationToken)
        {
            token = cancellationToken;
        }
    }
}