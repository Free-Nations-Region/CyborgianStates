using CyborgianStates.CommandHandling;
using CyborgianStates.Commands;
using CyborgianStates.Interfaces;
using CyborgianStates.MessageHandling;
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
        private readonly IResponseBuilder _responseBuilder;
        private readonly AppSettings _appSettings;
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
            _responseBuilder = _serviceProvider.GetRequiredService<IResponseBuilder>();
            _appSettings = _serviceProvider.GetRequiredService<IOptions<AppSettings>>().Value;
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
            CommandHandler.Register(new CommandDefinition(typeof(PingCommand), new List<string>() { "ping" }));
            CommandHandler.Register(new CommandDefinition(typeof(NationStatsCommand), new List<string>() { "nation", "n" }));
            CommandHandler.Register(new CommandDefinition(typeof(AboutCommand), new List<string>() { "about" }));
            CommandHandler.Register(new CommandDefinition(typeof(RegionStatsCommand), new List<string>() { "region", "r" }));
            CommandHandler.Register(new CommandDefinition(typeof(CouldEndorseCommand), new List<string>() { "couldendorse", "ce" }));
        }

        private async Task<bool> IsRelevantAsync(Message message)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));
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