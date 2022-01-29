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
        private readonly IUserRepository _userRepo;
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
            _userRepo = _serviceProvider.GetRequiredService<IUserRepository>();
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
                    Description = "Gets you some cool info about a NationStates Nation.",
                    IsSlashCommand = true,
                    SlashCommandParameters = new List<SlashCommandParameter>
                    {
                        new SlashCommandParameter(){ Name = "name", Type = ApplicationCommandOptionType.String, IsRequired = true, Description = "The nation name" }
                    },
                    SlashCommandPermissions = DefaultSlashCommandPermissions()
                    //IsGlobalSlashCommand = true,
                });
            CommandHandler.Register(
                new CommandDefinition(typeof(AboutCommand), new List<string>() { "about" })
                {
                    Name = "about",
                    Description = "Let me tell you something about myself.",
                    IsSlashCommand = true,
                    SlashCommandPermissions = DefaultSlashCommandPermissions()
                    /*IsGlobalSlashCommand = true*/
                });
            CommandHandler.Register(
                new CommandDefinition(typeof(RegionStatsCommand), new List<string>() { "region", "r" })
                {
                    Name = "region",
                    Description = "Get you some cool info about a NationStates Region.",
                    IsSlashCommand = true,
                    SlashCommandParameters = new List<SlashCommandParameter>
                    {
                        new SlashCommandParameter(){ Name = "name", Type = ApplicationCommandOptionType.String, IsRequired = false, Description = "The region name" }
                    },
                    SlashCommandPermissions = DefaultSlashCommandPermissions()
                    //IsGlobalSlashCommand = true
                });
            ;
            // Determine Permissions from DB
        }

        private static List<SlashCommandPermission> DefaultSlashCommandPermissions() => new()
        {
            new SlashCommandPermission() { Id = 730872813010813080, IsUser = false },
            new SlashCommandPermission() { Id = 264147949167050752, IsUser = false },
        };

        private async Task<bool> IsRelevantAsync(Message message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (message.AuthorId != 0 && !await _userRepo.IsUserInDbAsync(message.AuthorId).ConfigureAwait(false))
            {
                await _userRepo.AddUserToDbAsync(message.AuthorId).ConfigureAwait(false);
            }
            var value = !string.IsNullOrWhiteSpace(message.Content);
            return value &&
                (message.AuthorId == 0 ||
                await _userRepo.IsAllowedAsync(
                    AppSettings.Configuration == "development" ? "Commands.Preview.Execute" : "Commands.Execute",
                    message.AuthorId).ConfigureAwait(false));
        }

        private async Task ProcessMessageAsync(MessageReceivedEventArgs e)
        {
            try
            {
                if (IsRunning)
                {
                    if (await IsRelevantAsync(e.Message).ConfigureAwait(false))
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
                    else
                    {
                        if (e.Message.IsSlashCommand)
                        {
                            await e.Message.ReplyAsync("Hmm...You don't seem to have the permission to execute that command. Sorry. :(").ConfigureAwait(false);
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
            _backgroundServiceRegistry.Register(new DumpRetrievalBackgroundService(_serviceProvider));
        }
    }
}