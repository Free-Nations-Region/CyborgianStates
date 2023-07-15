using CyborgianStates.Interfaces;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CyborgianStates.Wrapper
{
    public class SlashCommand : ISlashCommand
    {
        private readonly SocketSlashCommand _socketSlashCommand;
        public SlashCommand(SocketSlashCommand socketSlashCommand) => _socketSlashCommand = socketSlashCommand;

        public bool HasResponded => _socketSlashCommand.HasResponded;

        public IApplicationCommandInteractionData Data => _socketSlashCommand.Data;

        public string CommandName => _socketSlashCommand.CommandName;

        public ISocketMessageChannel Channel => _socketSlashCommand.Channel;

        public ulong Id => _socketSlashCommand.Id;

        public InteractionType Type => _socketSlashCommand.Type;

        public string Token => _socketSlashCommand.Token;

        public int Version => _socketSlashCommand.Version;

        public IUser User => _socketSlashCommand.User;

        public DateTimeOffset CreatedAt => _socketSlashCommand.CreatedAt;

        public string UserLocale => "en-us";

        public string GuildLocale => "en-us";

        public bool IsDMInteraction => _socketSlashCommand.IsDMInteraction;

        public ulong? ChannelId => _socketSlashCommand.ChannelId;

        public ulong? GuildId => _socketSlashCommand.GuildId;

        public ulong ApplicationId => _socketSlashCommand.ApplicationId;

        IDiscordInteractionData IDiscordInteraction.Data => _socketSlashCommand.Data;

        public Task DeferAsync(bool ephemeral = false, RequestOptions options = null) => _socketSlashCommand.DeferAsync(ephemeral, options);
        public Task DeleteOriginalResponseAsync(RequestOptions options = null) => throw new NotImplementedException();
        public async Task<IUserMessage> FollowupAsync(string text = null, Embed[] embeds = null, bool isTTS = false, bool ephemeral = false, AllowedMentions allowedMentions = null, MessageComponent components = null, Embed embed = null, RequestOptions options = null)
            => await _socketSlashCommand.FollowupAsync(text, embeds, isTTS, ephemeral, allowedMentions, components, embed, options).ConfigureAwait(false);
        public async Task<IUserMessage> FollowupWithFileAsync(Stream fileStream, string fileName, string text = null, Embed[] embeds = null, bool isTTS = false, bool ephemeral = false, AllowedMentions allowedMentions = null, MessageComponent components = null, Embed embed = null, RequestOptions options = null)
            => await _socketSlashCommand.FollowupWithFileAsync(fileStream, fileName, text, embeds, isTTS, ephemeral, allowedMentions, components, embed, options).ConfigureAwait(false);
        public async Task<IUserMessage> FollowupWithFileAsync(string filePath, string fileName = null, string text = null, Embed[] embeds = null, bool isTTS = false, bool ephemeral = false, AllowedMentions allowedMentions = null, MessageComponent components = null, Embed embed = null, RequestOptions options = null)
            => await _socketSlashCommand.FollowupWithFileAsync(filePath, fileName, text, embeds, isTTS, ephemeral, allowedMentions, components, embed, options).ConfigureAwait(false);
        public async Task<IUserMessage> FollowupWithFileAsync(FileAttachment attachment, string text = null, Embed[] embeds = null, bool isTTS = false, bool ephemeral = false, AllowedMentions allowedMentions = null, MessageComponent components = null, Embed embed = null, RequestOptions options = null)
            => await _socketSlashCommand.FollowupWithFileAsync(attachment, text, embeds, isTTS, ephemeral, allowedMentions, components, embed, options).ConfigureAwait(false);
        public async Task<IUserMessage> FollowupWithFilesAsync(IEnumerable<FileAttachment> attachments, string text = null, Embed[] embeds = null, bool isTTS = false, bool ephemeral = false, AllowedMentions allowedMentions = null, MessageComponent components = null, Embed embed = null, RequestOptions options = null)
            => await _socketSlashCommand.FollowupWithFilesAsync(attachments, text, embeds, isTTS, ephemeral, allowedMentions, components, embed, options).ConfigureAwait(false);
        public async Task<IUserMessage> GetOriginalResponseAsync(RequestOptions options = null) => await _socketSlashCommand.GetOriginalResponseAsync(options).ConfigureAwait(false);
        public async Task<IUserMessage> ModifyOriginalResponseAsync(Action<MessageProperties> func, RequestOptions options = null)
            => await _socketSlashCommand.ModifyOriginalResponseAsync(func, options).ConfigureAwait(false);
        public async Task RespondAsync(string text = null, Embed[] embeds = null, bool isTTS = false, bool ephemeral = false, AllowedMentions allowedMentions = null, MessageComponent components = null, Embed embed = null, RequestOptions options = null)
            => await _socketSlashCommand.RespondAsync(text, embeds, isTTS, ephemeral, allowedMentions, components, embed, options).ConfigureAwait(false);
        public async Task RespondWithFilesAsync(IEnumerable<FileAttachment> attachments, string text = null, Embed[] embeds = null, bool isTTS = false, bool ephemeral = false, AllowedMentions allowedMentions = null, MessageComponent components = null, Embed embed = null, RequestOptions options = null) 
            => await _socketSlashCommand.RespondWithFilesAsync(attachments, text, embeds, isTTS, ephemeral, allowedMentions, components, embed, options).ConfigureAwait(false);
        public async Task RespondWithModalAsync(Modal modal, RequestOptions options = null) => await _socketSlashCommand.RespondWithModalAsync(modal, options).ConfigureAwait(false);
        async Task IDiscordInteraction.RespondAsync(string text, Embed[] embeds, bool isTTS, bool ephemeral, AllowedMentions allowedMentions, MessageComponent components, Embed embed, RequestOptions options) 
            => await (_socketSlashCommand as IDiscordInteraction).RespondAsync(text, embeds, isTTS, ephemeral, allowedMentions, components, embed, options).ConfigureAwait(false);
    }
}