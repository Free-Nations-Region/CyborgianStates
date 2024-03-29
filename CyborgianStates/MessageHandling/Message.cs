﻿using CyborgianStates.CommandHandling;
using CyborgianStates.Interfaces;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace CyborgianStates.MessageHandling
{
    public class Message
    {
        public Message(ulong authorId, string content, IMessageChannel channel, object msgObj)
        {
            AuthorId = authorId;
            Content = content;
            Channel = channel;
            MessageObject = msgObj;
        }

        public Message(ulong authorId, string content, IMessageChannel channel) : this(authorId, content, channel, null)
        {
        }

        public object MessageObject { get; }
        public ulong AuthorId { get; }
        public IMessageChannel Channel { get; }
        public string Content { get; }

        public bool HasResponded { get; private set; }

        public ISlashCommand SlashCommand => MessageObject as ISlashCommand;
        public bool IsSlashCommand => MessageObject is ISlashCommand;

        public async Task DeferAsync()
        {
            if (IsSlashCommand)
            {
                await SlashCommand.DeferAsync().ConfigureAwait(false);
            }
        }

        public async Task ReplyAsync(CommandResponse response, bool isPublic = true)
        {
            if (IsSlashCommand)
            {
                if (SlashCommand.HasResponded)
                {
                    await SlashCommand.ModifyOriginalResponseAsync(f => { f.Content = response.Content; f.Embed = response.ResponseObject; }).ConfigureAwait(false);
                }
                else
                {
                    await SlashCommand.RespondAsync(text: response.Content, embed: response.ResponseObject, ephemeral: !isPublic).ConfigureAwait(false);
                }
            }
            else
            {
                await Channel.ReplyToAsync(this, response, isPublic).ConfigureAwait(false);
            }
            HasResponded = true;
        }

        public async Task ReplyAsync(string content, bool isPublic = true)
        {
            if (IsSlashCommand)
            {
                if (SlashCommand.HasResponded)
                {
                    await SlashCommand.RespondAsync(text: content, ephemeral: isPublic).ConfigureAwait(false);
                }
                else
                {
                    await SlashCommand.RespondAsync(text: content, ephemeral: isPublic).ConfigureAwait(false);
                }

            }
            else
            {
                await Channel.ReplyToAsync(this, content, isPublic).ConfigureAwait(false);
            }
            HasResponded = true;
        }
    }
}