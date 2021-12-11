using CyborgianStates.CommandHandling;
using CyborgianStates.Interfaces;
using CyborgianStates.MessageHandling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CyborgianStates.Commands
{
    public class AboutCommand : BaseCommand
    {

        public AboutCommand() : base()
        {
        }

        public AboutCommand(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public override Task<CommandResponse> Execute(Message message)
        {
            var response = _responseBuilder.Success()
                .WithTitle("About CyborgianStates")
                .WithField("Contact for this instance", _config.Contact)
                .WithField("Github", "[CyborgianStates](https://github.com/Free-Nations-Region/CyborgianStates)")
                .WithField("Developed by Drehtisch", $"Discord: Drehtisch#5680{Environment.NewLine}NationStates: [Tigerania](https://www.nationstates.net/nation=tigerania)")
                .WithField("Support", "via [OpenCollective](https://opencollective.com/fnr)")
                .WithDefaults(_config.Footer).Build();
            return Task.FromResult(response);
        }

        public void SetCancellationToken(CancellationToken cancellationToken)
        {
        }
    }
}