using System;
using System.Threading;
using System.Threading.Tasks;
using CyborgianStates.Enums;
using CyborgianStates.Interfaces;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
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

        public DiscordMessageHandler(IOptions<AppSettings> options, DiscordClientWrapper socketClient)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            _logger = Log.ForContext<DiscordMessageHandler>();
            ;
            _client = socketClient ?? throw new ArgumentNullException(nameof(socketClient));
            _settings = options.Value;
        }

        public bool IsRunning { get; private set; }

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public async Task InitAsync()
        {
            _logger.Information("-- DiscordMessageHandler Init --");
            await _client.LoginAsync(TokenType.Bot, _settings.DiscordBotLoginToken).ConfigureAwait(false);
            SetupDiscordEvents();
        }

        private void SetupDiscordEvents()
        {
            _client.Connected += Discord_Connected;
            _client.Disconnected += Discord_Disconnected;
            _client.Log += Discord_Log;
            _client.MessageReceived += Discord_MessageReceived;
            _client.LoggedIn += Discord_LoggedIn;
            _client.LoggedOut += Discord_LoggedOut;
            _client.Ready += Discord_Ready;
        }

        private Task Discord_Ready()
        {
            _logger.Information("--- Discord Client Ready ---");
            return Task.CompletedTask;
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

        internal Task HandleMessage(IMessage message)
        {
            _logger.Verbose("ChannelId: {channelId} {name} {message}", message.Channel.Id, message.Channel.Name, message.Content);
            if (message.Content.StartsWith(_settings.SeperatorChar))
            {
                var usermsg = message as SocketUserMessage;
                var msgContent = message.Content[1..];
                var context = usermsg != null ? new SocketCommandContext(_client, usermsg) : null;
                var isPrivate = usermsg != null && context.IsPrivate;
                MessageReceived?.Invoke(this,
                    new MessageReceivedEventArgs(
                        new Message(
                            message.Author.Id,
                            msgContent,
                            new DiscordMessageChannel(message.Channel, isPrivate),
                            context
                )));
            }
            return Task.CompletedTask;
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