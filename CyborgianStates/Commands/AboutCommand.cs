﻿using CyborgianStates.CommandHandling;
using CyborgianStates.Interfaces;
using CyborgianStates.MessageHandling;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CyborgianStates.Commands
{
    public class AboutCommand : ICommand
    {
        private readonly AppSettings _config;
        private readonly IResponseBuilder _responseBuilder;
        public AboutCommand()
        {
            _responseBuilder = (IResponseBuilder) Program.ServiceProvider.GetService(typeof(IResponseBuilder));
            _config = ((IOptions<AppSettings>) Program.ServiceProvider.GetService(typeof(IOptions<AppSettings>))).Value;
        }
        public async Task<CommandResponse> Execute(Message message)
        {
            var response = _responseBuilder.Success()
                .WithTitle("About CyborgianStates")
                .WithField("Contact for this instance", _config.Contact)
                .WithField("Github", "[CyborgianStates](https://github.com/Free-Nations-Region/CyborgianStates)")
                .WithField("Developed by Drehtisch", $"Discord: Drehtisch#5680{Environment.NewLine}NationStates: [Tigerania](https://www.nationstates.net/nation=tigerania)")
                .WithField("Support", "via [OpenCollective](https://opencollective.com/fnr)")
                .WithDefaults(_config.Footer).Build();
            await message.Channel.ReplyToAsync(message, response).ConfigureAwait(false);
            return response;
        }
        public void SetCancellationToken(CancellationToken cancellationToken) { }
    }
}