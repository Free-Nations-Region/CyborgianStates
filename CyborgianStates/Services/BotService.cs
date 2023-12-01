using CyborgianStates.CommandHandling;
using CyborgianStates.Commands;
using CyborgianStates.Interfaces;
using CyborgianStates.MessageHandling;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NationStatesSharp.Interfaces;
using Quartz;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ILogger = Serilog.ILogger;

namespace CyborgianStates.Services
{
    public class BotService : IBotService
    {
        private readonly ILogger _logger;
        private readonly IMessageHandler _messageHandler;
        private readonly IRequestDispatcher _requestDispatcher;
        private readonly IBackgroundServiceRegistry _backgroundServiceRegistry;
        private readonly IServiceProvider _serviceProvider;
        public BotService() : this(Program.ServiceProvider)
        {
        }

        public BotService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _messageHandler = _serviceProvider.GetRequiredService<IMessageHandler>();
            _requestDispatcher = _serviceProvider.GetRequiredService<IRequestDispatcher>();
            _logger = Log.Logger.ForContext<BotService>();
            _backgroundServiceRegistry = _serviceProvider.GetRequiredService<IBackgroundServiceRegistry>();
        }

        public bool IsRunning { get; private set; }

        public async Task InitAsync()
        {
            _logger.Information("BotService Initializing");
            Register();
            _messageHandler.MessageReceived += async (s, e) => await ProcessMessageAsync(e).ConfigureAwait(false);
            await _messageHandler.InitAsync().ConfigureAwait(false);
        }

        public async Task RunAsync()
        {
            _logger.Information("BotService Starting");
            IsRunning = true;
            _requestDispatcher.Start();
            await _backgroundServiceRegistry.StartAsync().ConfigureAwait(false);
            _logger.Information("BotService Running");
            await _messageHandler.RunAsync().ConfigureAwait(false);
        }

        public async Task ShutdownAsync()
        {
            _logger.Information("BotService Shutdown");
            CommandHandler.Cancel();
            _requestDispatcher.Shutdown();
            await _messageHandler.ShutdownAsync().ConfigureAwait(false);
            await _backgroundServiceRegistry.ShutdownAsync().ConfigureAwait(false);
            IsRunning = false;
            _logger.Information("BotService Stopped");
        }

        private static void RegisterCommands()
        {
            CommandHandler.Register(
                new CommandDefinition(typeof(PingCommand), new List<string>() { "ping" }) { Name = "ping", Description = "Pong's you, lol.", IsSlashCommand = true, /*IsGlobalSlashCommand = true*/ });
            CommandHandler.Register(
                new CommandDefinition(typeof(NationStatsCommand), new List<string>() { "nation", "n" })
                {
                    Name = "nation",
                    Description = "Gets you some info about a NationStates Nation.",
                    IsSlashCommand = true,
                    SlashCommandParameters = new List<SlashCommandParameter>
                    {
                        new SlashCommandParameter(){ Name = "name", Type = ApplicationCommandOptionType.String, IsRequired = true, Description = "The nation name" }
                    }
                });
            CommandHandler.Register(
                new CommandDefinition(typeof(AboutCommand), new List<string>() { "about" })
                {
                    Name = "about",
                    Description = "Let me tell you something about myself.",
                    IsSlashCommand = true
                });
            CommandHandler.Register(
                new CommandDefinition(typeof(RegionStatsCommand), new List<string>() { "region", "r" })
                {
                    Name = "region",
                    Description = "Get you some info about a NationStates Region.",
                    IsSlashCommand = true,
                    SlashCommandParameters = new List<SlashCommandParameter>
                    {
                        new SlashCommandParameter(){ Name = "name", Type = ApplicationCommandOptionType.String, IsRequired = false, Description = "The region name" }
                    },
                });
            CommandHandler.Register(
                new CommandDefinition(typeof(CouldEndorseCommand), new List<string>() { "couldendorse", "ce" })
                {
                    Name = "couldendorse",
                    Description = "Tells you what nations you could endorse",
                    IsSlashCommand = true,
                    SlashCommandParameters = new List<SlashCommandParameter>
                    {
                        new SlashCommandParameter(){ Name = "name", Type = ApplicationCommandOptionType.String, IsRequired = true, Description = "The nation name" }
                    },
                });
        }

        private async Task<bool> IsRelevantAsync(Message message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return !string.IsNullOrWhiteSpace(message.Content);
        }

        private async Task ProcessMessageAsync(MessageReceivedEventArgs e)
        {
            try
            {
                if (IsRunning)
                {
                    var result = await CommandHandler.ExecuteAsync(e.Message).ConfigureAwait(false);
                    if (result == null)
                    {
                        _logger.Error($"Unknown command trigger {e.Message.Content}");
                        if (e.Message.IsSlashCommand)
                        {
                            await e.Message.ReplyAsync("Hmm...That didn't work. I don't know what to do. This is not intended. Please contact BotAdmin to get this fixed.").ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        if (!e.Message.HasResponded)
                        {
                            await e.Message.ReplyAsync(result).ConfigureAwait(false);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex, $"Unexpected error occured while Processing Message -> {e.Message}: ");
            }
        }

        private void Register()
        {
            RegisterCommands();
            _backgroundServiceRegistry.Register(_serviceProvider.GetRequiredService<DumpRetrievalBackgroundService>());
        }
    }
}