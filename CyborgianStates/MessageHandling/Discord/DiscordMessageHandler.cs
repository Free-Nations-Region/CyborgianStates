using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CyborgianStates.CommandHandling;
using CyborgianStates.Data.Models;
using CyborgianStates.Enums;
using CyborgianStates.Interfaces;
using CyborgianStates.Wrapper;
using Dapper.Contrib.Extensions;
using DataAbstractions.Dapper;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using ILogger = Serilog.ILogger;

namespace CyborgianStates.MessageHandling
{
    public class DiscordMessageHandler : IMessageHandler
    {
        private readonly ILogger _logger;
        private readonly DiscordClientWrapper _client;
        private readonly AppSettings _settings;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly SemaphoreSlim _semaphore = new(0, 1);
        private readonly IDataAccessor _dataAccessor;
        public DiscordMessageHandler(IOptions<AppSettings> options, DiscordClientWrapper socketClient, IDataAccessor dataAccessor)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            _logger = Log.ForContext<DiscordMessageHandler>();
            _client = socketClient ?? throw new ArgumentNullException(nameof(socketClient));
            _settings = options.Value;
            _dataAccessor = dataAccessor ?? throw new ArgumentNullException(nameof(dataAccessor));
        }

        public bool IsRunning { get; private set; }

        private bool _isRegistered = false;
        private const bool _skipUpdate = true;
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public async Task InitAsync()
        {
            _logger.Information("-- DiscordMessageHandler Init --");
            SetupDiscordEvents();
            await _client.LoginAsync(TokenType.Bot, _settings.DiscordBotLoginToken).ConfigureAwait(false);
        }

        private async Task RegisterSlashCommandsWithPermissionsAsync()
        {
            try
            {
                if (!_isRegistered && !_skipUpdate)
                {
                    var restClient = _client.Rest;
                    var commandsToBeRegistered = CommandHandler.Definitions.Where(d => d.IsSlashCommand);
                    var guildCommands = await restClient.GetGuildApplicationCommands(AppSettings.PrimaryGuildId).ConfigureAwait(false);
                    var globalCommands = await restClient.GetGlobalApplicationCommands().ConfigureAwait(false);
                    var guild = _client.GetGuild(AppSettings.PrimaryGuildId);
                    foreach (var command in commandsToBeRegistered)
                    {
                        _logger.Information("Registering command '{commandName}'", command.Name);
                        var slashCommand = GetSlashCommandFromCommandDefinition(command);
                        if (command.IsGlobalSlashCommand)
                        {
                            if (globalCommands.Any(a => a.Name == command.Name))
                            {
                                _logger.Information("Command '{commandName}' is already registered as global command. Updating.", command.Name);
                                var gCommand = globalCommands.First(a => a.Name == command.Name);
                                await gCommand.ModifyAsync<SlashCommandProperties>(f =>
                                {
                                    f.Name = slashCommand.Name;
                                    f.Description = slashCommand.Description;
                                    f.Options = slashCommand.Options;
                                    f.IsDefaultPermission = slashCommand.IsDefaultPermission;
                                }).ConfigureAwait(false);
                                _logger.Information("Command '{commandName}' updated sucessfully.", command.Name);
                            }
                            else
                            {
                                await restClient.CreateGlobalCommand(slashCommand).ConfigureAwait(false);
                                _logger.Information("Command '{commandName}' created sucessfully.", command.Name);
                            }
                        }
                        else
                        {
                            RestGuildCommand resultCommand;
                            if (guildCommands.Any(a => a.Name == command.Name))
                            {
                                var gCommand = guildCommands.First(a => a.Name == command.Name);
                                resultCommand = gCommand;
                                _logger.Information("Command '{commandName}' is already registered as guild command for the primary guild. Updating.", command.Name);
                                await gCommand.ModifyAsync<SlashCommandProperties>(f =>
                                {
                                    f.Name = slashCommand.Name;
                                    f.Description = slashCommand.Description;
                                    f.Options = slashCommand.Options;
                                    f.IsDefaultPermission = slashCommand.IsDefaultPermission;
                                }).ConfigureAwait(false);
                                _logger.Information("Command '{commandName}' updated sucessfully.", command.Name);
                            }
                            else
                            {
                                resultCommand = await restClient.CreateGuildCommand(slashCommand, AppSettings.PrimaryGuildId).ConfigureAwait(false);
                                _logger.Information("Command '{commandName}' created sucessfully.", command.Name);
                            }
                            if (!resultCommand.IsDefaultPermission)
                            {
                                _logger.Information("Updating permissions for command '{commandName}'.", command.Name);
                                await resultCommand.ModifyCommandPermissions(GetApplicationCommandPermissions(command, _logger.ForContext("command", command.Name)).ToArray()).ConfigureAwait(false);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex, "Error while registering commands.");
            }
        }

        private static IEnumerable<ApplicationCommandPermission> GetApplicationCommandPermissions(CommandDefinition definition, ILogger logger)
        {
            if (definition.IsSlashCommand)
            {
                foreach (var perm in definition.SlashCommandPermissions)
                {
                    logger.Information("Permission Type {type} Id {id}.", perm.IsUser ? ApplicationCommandPermissionTarget.User : ApplicationCommandPermissionTarget.Role, perm.Id);
                    yield return new ApplicationCommandPermission(perm.Id, perm.IsUser ? ApplicationCommandPermissionTarget.User : ApplicationCommandPermissionTarget.Role, true);
                }
            }
            else
            {
                yield break;
            }
        }

        private static SlashCommandProperties GetSlashCommandFromCommandDefinition(CommandDefinition definition)
        {
            var commandBuilder = new Discord.SlashCommandBuilder();
            commandBuilder.WithName(definition.Name);
            commandBuilder.WithDescription(definition.Description);
            commandBuilder.IsDefaultPermission = !definition.SlashCommandPermissions.Any();
            foreach (var param in definition.SlashCommandParameters)
            {
                commandBuilder.AddOption(param.Name, param.Type, param.Description, isRequired: param.IsRequired);
            }
            var res = commandBuilder.Build();
            return res;
        }

        private void SetupDiscordEvents()
        {
            _client.Connected += Discord_Connected;
            _client.Disconnected += Discord_Disconnected;
            _client.Log += Discord_Log;
            _client.MessageReceived += Discord_MessageReceived;
            _client.LoggedIn += Discord_LoggedIn;
            _client.LoggedOut += Discord_LoggedOut;
            _client.Ready += Discord_ReadyAsync;
            _client.SlashCommandExecuted += Discord_SlashCommandExecuted;
            _client.ButtonExecuted += Client_ButtonExecutedAsync;
        }

        private async Task Client_ButtonExecutedAsync(SocketMessageComponent arg)
        {
            if (arg is not null)
            {
                //if (_logger.IsEnabled(LogEventLevel.Verbose))
                //{
                //    _logger.Verbose("Message Component >> ChannelId: {channelId} {name} {author} {message}", arg.Channel?.Id, arg.User?.Username, arg.Channel?.Name, arg.Data.CustomId);
                //}
                //MessageReceived?.Invoke(this,
                //    new MessageReceivedEventArgs(
                //        new Message(
                //            arg.User.Id,
                //            arg.Data.CustomId,
                //            new DiscordMessageChannel(arg.Channel, false),
                //            arg
                //)));
            }
        }

        private Task Discord_SlashCommandExecuted(SocketSlashCommand arg) => HandleSlashCommandAsync(new SlashCommand(arg));

        internal async Task HandleSlashCommandAsync(ISlashCommand arg)
        {
            if (arg is not null)
            {
                if (_logger.IsEnabled(LogEventLevel.Verbose))
                {
                    _logger.Verbose("SlashCommand >> ChannelId: {channelId} {name} {author} {message}", arg.Channel?.Id, arg.User?.Username, arg.Channel?.Name, arg.CommandName);
                }
                var stringBuilder = new StringBuilder();
                stringBuilder.Append(arg.Data.Name);
                foreach (var opt in arg.Data.Options)
                {
                    stringBuilder.Append(' ');
                    stringBuilder.Append(opt.Name);
                    stringBuilder.Append(": ");
                    stringBuilder.Append(opt.Value);
                }
                var usage = new CommandUsage()
                {
                    TraceId = arg.Id.ToString(),
                    UserId = arg.User.Id,
                    ChannelId = arg.Channel.Id,
                    Command = stringBuilder.ToString(),
                    CommandType = CommandType.SlashCommand,
                    IsPrimaryGuild = true,
                    IsDM = false,
                    Timestamp = arg.CreatedAt.ToUnixTimeSeconds(),

                };
                await _dataAccessor.InsertAsync(usage).ConfigureAwait(false);
                MessageReceived?.Invoke(this,
                    new MessageReceivedEventArgs(
                        new Message(
                            arg.User.Id,
                            arg.Data.Name,
                            new DiscordMessageChannel(arg.Channel, false),
                            arg
                )));
            }

            //var builder = new ComponentBuilder().WithButton("Test Button", "custom-id");
            //await arg.DeferAsync().ConfigureAwait(false);
            //await arg.ModifyOriginalResponseAsync(f => { f.Components = builder.Build(); f.Content = "Test Reply"; }).ConfigureAwait(false);

        }

        private async Task Discord_ReadyAsync()
        {
            _logger.Information("--- Discord Client Ready ---");
            await RegisterSlashCommandsWithPermissionsAsync().ConfigureAwait(false);
        }

        private Task Discord_LoggedOut()
        {
            _logger.Information("--- Bot logged out ---");
            return Task.CompletedTask;
        }

        private Task Discord_LoggedIn()
        {
            _logger.Information("--- Bot logged in ---");
            return Task.CompletedTask;
        }

        private Task Discord_MessageReceived(SocketMessage arg) => HandleMessage(arg);

        internal async Task HandleMessage(IMessage message)
        {
            if (message.Content.StartsWith(_settings.SeperatorChar))
            {
                var usermsg = message as SocketUserMessage;

                var msgContent = message.Content[1..];
                var context = usermsg != null ? new SocketCommandContext(_client, usermsg) : null;
                var isPrivate = usermsg != null && context.IsPrivate;

                var usage = new CommandUsage()
                {
                    TraceId = message.Id.ToString(),
                    UserId = message.Author.Id,
                    ChannelId = message.Channel.Id,
                    Command = msgContent,
                    CommandType = CommandType.Message,
                    IsPrimaryGuild = AppSettings.PrimaryGuildId == context.Guild?.Id,
                    GuildId = AppSettings.PrimaryGuildId == context.Guild?.Id ? 0 : context.Guild?.Id ?? 0,
                    IsDM = isPrivate,
                    Timestamp = message.CreatedAt.ToUnixTimeSeconds()
                };
                await _dataAccessor.InsertAsync(usage).ConfigureAwait(false);

                MessageReceived?.Invoke(this,
                    new MessageReceivedEventArgs(
                        new Message(
                            message.Author.Id,
                            msgContent,
                            new DiscordMessageChannel(message.Channel, isPrivate),
                            context
                )));
            }
        }

        private Task Discord_Log(LogMessage arg)
        {
            var id = Helpers.GetEventIdByType(LoggingEvent.DiscordLogEvent);
            string message = LogMessageBuilder.Build(id, $"[{arg.Source}] {arg.Message}");
            switch (arg.Severity)
            {
                case LogSeverity.Critical:
                    _logger.Fatal(message);
                    break;

                case LogSeverity.Error:
                    _logger.Error(message);
                    break;

                case LogSeverity.Warning:
                    _logger.Warning(message);
                    break;

                case LogSeverity.Info:
                    _logger.Information(message);
                    break;

                default:
                    _logger.Debug($"Severity: {arg.Severity} {message}");
                    break;
            }
            return Task.CompletedTask;
        }

        private Task Discord_Disconnected(Exception arg)
        {
            _logger.Warning(arg, "--- Disconnected from Discord ---");
            return Task.CompletedTask;
        }

        private Task Discord_Connected()
        {
            _logger.Information("--- Connected to Discord ---");
            return Task.CompletedTask;
        }

        public async Task RunAsync()
        {
            await _client.StartAsync().ConfigureAwait(false);
            IsRunning = true;
            await _semaphore.WaitAsync().ConfigureAwait(false);
        }

        public async Task ShutdownAsync()
        {
            await _client.LogoutAsync().ConfigureAwait(false);
            await _client.StopAsync().ConfigureAwait(false);
            IsRunning = false;
            _semaphore.Release();
            _client.Dispose();
        }
    }

}