using System;
using System.Threading;
using System.Threading.Tasks;
using CyborgianStates.CommandHandling;
using CyborgianStates.Interfaces;
using CyborgianStates.MessageHandling;

namespace CyborgianStates.Commands
{
    public class PingCommand : ICommand
    {
        public Task<CommandResponse> Execute(Message message)
        {
            return message is null
                ? throw new ArgumentNullException(nameof(message))
                : Task.FromResult(new CommandResponse(CommandStatus.Success, "Pong !"));
        }

        public void SetCancellationToken(CancellationToken cancellationToken)
        {
        }
    }
}