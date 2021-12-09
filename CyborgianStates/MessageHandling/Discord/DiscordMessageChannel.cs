using System;
using System.Threading.Tasks;
using CyborgianStates.CommandHandling;
using CyborgianStates.Interfaces;
using Discord.Commands;
using Discord.WebSocket;

namespace CyborgianStates.MessageHandling
{
    public class DiscordMessageChannel : IMessageChannel
    {
        private readonly Discord.IMessageChannel _channel;
        private Discord.IMessageChannel currentChannel;

        public DiscordMessageChannel(Discord.IMessageChannel channel, bool isPrivate)
        {
            _channel = channel;
            currentChannel = _channel;
            _isPrivateChannel = isPrivate;
        }

        private readonly bool _isPrivateChannel;

        public async Task ReplyToAsync(Message message, string content)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));
            await ReplyToAsync(message, content, true).ConfigureAwait(false);
        }

        public async Task ReplyToAsync(Message message, CommandResponse response)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));
            if (response is null)
                throw new ArgumentNullException(nameof(response));
            await ReplyToAsync(message, response, true).ConfigureAwait(false);
        }

        public async Task ReplyToAsync(Message message, string content, bool isPublic)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            if (message.MessageObject is SocketSlashCommand command)
            {
                await command.ModifyOriginalResponseAsync(f => { f.Content = content; }).ConfigureAwait(false);
                //await command.RespondAsync(text: content, ephemeral: isPublic).ConfigureAwait(false);
            }
            else
            {
                currentChannel = !isPublic && !_isPrivateChannel
                ? message.MessageObject is SocketCommandContext Context ? await Context.User.CreateDMChannelAsync().ConfigureAwait(false) : _channel
                : _channel;
                await WriteToAsync(content).ConfigureAwait(false);
            }
        }

        public async Task ReplyToAsync(Message message, CommandResponse response, bool isPublic)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));
            if (response is null)
                throw new ArgumentNullException(nameof(response));
            if (message.MessageObject is SocketSlashCommand command)
            {
                await command.ModifyOriginalResponseAsync(f => { f.Content = response.Content; f.Embed = response.ResponseObject as Discord.Embed; }).ConfigureAwait(false);
                //await command.RespondAsync(text: response.Content, embed: response.ResponseObject as Discord.Embed, ephemeral: isPublic).ConfigureAwait(false);
            }
            else
            {
                currentChannel = !isPublic && !_isPrivateChannel
                ? message.MessageObject is SocketCommandContext Context ? await Context.User.CreateDMChannelAsync().ConfigureAwait(false) : _channel
                : _channel;
                await WriteToAsync(response).ConfigureAwait(false);
            }
        }

        public async Task WriteToAsync(CommandResponse response)
        {
            if (response is null)
                throw new ArgumentNullException(nameof(response));
            await currentChannel.SendMessageAsync(text: response.Content, embed: response.ResponseObject as Discord.Embed).ConfigureAwait(false);
        }

        public async Task WriteToAsync(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentNullException(nameof(content));
            await currentChannel.SendMessageAsync(text: content).ConfigureAwait(false);
        }
    }
}