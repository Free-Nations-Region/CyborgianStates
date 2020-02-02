﻿using CyborgianStates.CommandHandling;
using CyborgianStates.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CyborgianStates
{
    public class BotService
    {
        IMessageHandler _messageHandler;
        public BotService(IMessageHandler messageHandler)
        {
            _messageHandler = messageHandler;
        }
        public bool IsRunning { get; private set; }

        public async Task InitAsync()
        {
            _messageHandler.MessageReceived += async (s, e) => await ProgressMessage(s, e).ConfigureAwait(false);
            await _messageHandler.InitAsync().ConfigureAwait(false);
        }

        private async Task ProgressMessage(object sender, MessageReceivedEventArgs e)
        {
            //TODO: Relevance and Permission Check
            if (sender is IMessageHandler)
            {
                await Task.Run(() => CommandHandler.Execute(e.Message)).ConfigureAwait(false);
            }
        }

        public async Task RunAsync()
        {
            await _messageHandler.RunAsync().ConfigureAwait(false);
        }

        public async Task ShutdownAsync()
        {
            await _messageHandler.ShutdownAsync().ConfigureAwait(false);
        }
    }
}