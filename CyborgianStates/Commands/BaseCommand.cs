using CyborgianStates.CommandHandling;
using CyborgianStates.Interfaces;
using CyborgianStates.MessageHandling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NationStatesSharp.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CyborgianStates.Commands
{
    public abstract class BaseCommand : ICommand
    {
        protected readonly AppSettings _config;
        protected readonly IRequestDispatcher _dispatcher;
        protected readonly IResponseBuilder _responseBuilder;
        protected CancellationToken _token;

        public BaseCommand() : this(Program.ServiceProvider)
        {
        }

        public BaseCommand(IServiceProvider serviceProvider)
        {
            _dispatcher = serviceProvider.GetRequiredService<IRequestDispatcher>();
            _config = serviceProvider.GetRequiredService<IOptions<AppSettings>>().Value;
            _responseBuilder = serviceProvider.GetRequiredService<IResponseBuilder>();
        }

        public abstract Task<CommandResponse> Execute(Message message);
        public void SetCancellationToken(CancellationToken cancellationToken) => _token = cancellationToken;

        protected async Task<CommandResponse> FailCommandAsync(Message message, string reason)
        {
            return await FailCommandAsync(message, reason, Discord.Color.Red).ConfigureAwait(false);
        }
        protected async Task<CommandResponse> FailCommandAsync(Message message, string reason, Discord.Color color, string title = null)
        {
            _responseBuilder.Clear();
            _responseBuilder.WithFooter(_config.Footer)
                .FailWithDescription(reason)
                .WithColor(color);
            if (title != null)
            {
                _responseBuilder.WithTitle(title);
            }
            var response = _responseBuilder.Build();
            await message.Channel.ReplyToAsync(message, response).ConfigureAwait(false);
            return response;
        }
    }
}