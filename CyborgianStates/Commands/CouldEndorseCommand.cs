using CyborgianStates.CommandHandling;
using CyborgianStates.Interfaces;
using CyborgianStates.MessageHandling;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CyborgianStates.Commands
{
    public class CouldEndorseCommand : ICommand
    {
        private readonly IResponseBuilder _responseBuilder;
        private readonly ILogger _logger;
        private CancellationToken token;
        
        public CouldEndorseCommand() : this(Program.ServiceProvider)
        {
        }

        public CouldEndorseCommand(IServiceProvider serviceProvider)
        {
            _logger = Log.ForContext<CouldEndorseCommand>();
            _responseBuilder = serviceProvider.GetRequiredService<IResponseBuilder>();
        }

        public async Task<CommandResponse> Execute(Message message)
        {
            if (message is null)
            {
                throw new ArgumentException(nameof(message));
            }

            try
            {
                // TODO: Actually implement this.
                var response = new CommandResponse(CommandStatus.Success, "Pong !");
                await message.Channel.ReplyToAsync(message, response).ConfigureAwait(false);
                return response;
            }
            catch (Exception e)
            {
                _logger.Error(e.ToString());
                return await FailCommandAsync(message,
                    "An unexpected error occured. Please contact the bot administrator.").ConfigureAwait(false);
            }
        }
        
        public void SetCancellationToken(CancellationToken cancellationToken)
        {
            token = cancellationToken;
        }
        
        private async Task<CommandResponse> FailCommandAsync(Message message, string reason)
        {
            _responseBuilder.Clear();
            // TODO: Convert .Failed() method to .FailWithDescription(reason) for more detailed error handling.
            var response = _responseBuilder.Failed(reason).Build();
            await message.Channel.ReplyToAsync(message, response).ConfigureAwait(false);
            return response;
        }
    }
    
    
}

