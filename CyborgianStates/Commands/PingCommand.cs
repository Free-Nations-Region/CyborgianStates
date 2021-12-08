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

        public async Task<CommandResponse> Execute(Message message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            _logger.Debug("- Ping -");
            var response = new CommandResponse(CommandStatus.Success, "Pong !");
            await message.Channel.ReplyToAsync(message, response).ConfigureAwait(false);
            return response;
        }

        public void SetCancellationToken(CancellationToken cancellationToken)
        {
            token = cancellationToken;
        }
    }
}