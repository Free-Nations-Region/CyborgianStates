using CyborgianStates.CommandHandling;
using CyborgianStates.Commands;
using CyborgianStates.Interfaces;
using CyborgianStates.MessageHandling;
using DataAbstractions.Dapper;
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
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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

            var dataAccessor = _serviceProvider.GetRequiredService<IDataAccessor>();
            var options = _serviceProvider.GetRequiredService<IOptions<AppSettings>>();
            dataAccessor.ConnectionString = options?.Value?.DbConnection;

            using (var stream = File.OpenRead(Path.Join("Data", "Sqlite", "Sqlite_CreateDb.sql")))
            {
                var hash = await CalculateHashFromStreamAsync(stream).ConfigureAwait(true);
                using (var streamReader = new StreamReader(stream))
                {
                    if (hash == "fed40914bbcecf6487a7f8f5d4be349faa3cc5097146dd09f52058657f62d508")
                    {
                        stream.Position = 0;
                        var fileContent = await streamReader.ReadToEndAsync().ConfigureAwait(true);
                        _logger.Information("Seeding database");
                        var result = await dataAccessor.ExecuteAsync(fileContent).ConfigureAwait(true);
                    }
                    else
                    {
                        _logger.Warning("Skipped seeding database. Hash mismatch.");
                    }
                }

            }
            _messageHandler.MessageReceived += async (s, e) => await ProcessMessageAsync(e).ConfigureAwait(false);
            await _messageHandler.InitAsync().ConfigureAwait(false);
        }

        private async Task<string> CalculateHashFromStreamAsync(Stream fileStream)
        {
            using (var hasher = SHA256.Create())
            {
                var bytes = await hasher.ComputeHashAsync(fileStream).ConfigureAwait(false);
                var hash = string.Concat(bytes.Select(b => b.ToString("x2")));
                _logger.Verbose("Hash: {@existingHash}", hash);
                return hash;
            }
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
                new CommandDefinition(typeof(PingCommand), ["ping"]) { Name = "ping", Description = "Pong's you, lol.", IsSlashCommand = true, /*IsGlobalSlashCommand = true*/ });
            CommandHandler.Register(
                new CommandDefinition(typeof(NationStatsCommand), ["nation", "n"])
                {
                    Name = "nation",
                    Description = "Gets you some cool info about a NationStates Nation.",
                    IsSlashCommand = true,
                    SlashCommandParameters = new List<SlashCommandParameter>
                    {
                        new(){ Name = "name", Type = ApplicationCommandOptionType.String, IsRequired = true, Description = "The nation name" }
                    },
                    IsGlobalSlashCommand = true
                });
            CommandHandler.Register(
                new CommandDefinition(typeof(AboutCommand), ["about"])
                {
                    Name = "about",
                    Description = "Let me tell you something about myself.",
                    IsSlashCommand = true,
                    IsGlobalSlashCommand = true
                });
            CommandHandler.Register(
                new CommandDefinition(typeof(RegionStatsCommand), ["region", "r"])
                {
                    Name = "region",
                    Description = "Get you some cool info about a NationStates Region.",
                    IsSlashCommand = true,
                    SlashCommandParameters = new List<SlashCommandParameter>
                    {
                        new(){ Name = "name", Type = ApplicationCommandOptionType.String, IsRequired = false, Description = "The region name" }
                    },
                    IsGlobalSlashCommand = true
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