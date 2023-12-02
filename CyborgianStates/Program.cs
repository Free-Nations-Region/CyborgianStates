using CyborgianStates.Data;
using CyborgianStates.Interfaces;
using CyborgianStates.MessageHandling;
using CyborgianStates.Services;
using DataAbstractions.Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NationStatesSharp;
using NationStatesSharp.Interfaces;
using Serilog;
using Quartz;
using System;
using System.Threading.Tasks;
using CyborgianStates.Wrapper;
using System.Data;

namespace CyborgianStates
{
    public static class Program
    {
        private static ILauncher Launcher = new Launcher();
        private static IUserInput userInput = null;
        private static IMessageHandler messageHandler = null;
        internal static string InputChannel { get; set; }
        internal static IServiceProvider ServiceProvider { get; set; }

        public static async Task Main()
        {
            try
            {
                var serviceCollection = new ServiceCollection();
                ServiceProvider = ConfigureServices();
                Console.CancelKeyPress += async (s, e) => await Launcher.ShutdownAsync().ConfigureAwait(false);
                await Launcher.RunAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"A fatal error caused the bot to crash: {ex}");
            }
        }

        public static void SetLauncher(ILauncher launcher) => Launcher = launcher;

        public static void SetUserInput(IUserInput input)
        {
            if (input is null)
            {
                throw new ArgumentNullException(nameof(input));
            }
            userInput = input;
        }

        public static void SetMessageHandler(IMessageHandler handler)
        {
            if (handler is null)
            {
                throw new ArgumentNullException(nameof(handler));
            }
            messageHandler = handler;
        }

        public static IServiceProvider ConfigureServices()
        {
            var serviceCollection = new ServiceCollection();
            string configurationName = "production";
#if DEBUG
            configurationName = "development";
#endif
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile($"appsettings.{configurationName}.json", false, true)
                .Build();
            serviceCollection.AddOptions();
            serviceCollection.Configure<AppSettings>(configuration.GetSection("Configuration"));
            serviceCollection.AddSingleton(typeof(IConfiguration), configuration);
            ConfigureLogging(serviceCollection, configuration);
            // add services
            var channel = configuration.GetSection("Configuration").GetSection("InputChannel").Value;
            if (string.IsNullOrWhiteSpace(InputChannel))
            {
                InputChannel = channel;
            }
            if (InputChannel == "Console")
            {
                userInput ??= new ConsoleInput();
                messageHandler ??= new ConsoleMessageHandler(userInput);
                serviceCollection.AddSingleton(typeof(IUserInput), userInput);
                serviceCollection.AddSingleton(typeof(IMessageHandler), messageHandler);
                serviceCollection.AddTransient<IResponseBuilder, ConsoleResponseBuilder>();
            }
            else if (InputChannel == "Discord")
            {
                serviceCollection.AddSingleton(typeof(DiscordClientWrapper), new DiscordClientWrapper());
                serviceCollection.AddSingleton<IMessageHandler, DiscordMessageHandler>();
                serviceCollection.AddTransient<IResponseBuilder, DiscordResponseBuilder>();
            }
            else
            {
                throw new InvalidOperationException($"Unknown InputChannel '{InputChannel}'");
            }



            var requestDispatcher = new RequestDispatcher($"({configuration.GetSection("Configuration").GetSection("Contact").Value})", Log.Logger);

            
            var sc = new SqliteConnection();
            
            serviceCollection.AddSingleton(typeof(IRequestDispatcher), requestDispatcher);
            serviceCollection.AddSingleton<IBotService, BotService>();
            serviceCollection.AddSingleton(typeof(IDbConnection), sc);
            serviceCollection.AddSingleton<IDataAccessor, DataAccessor>();
            serviceCollection.AddSingleton<IDumpDataService, DumpDataService>();
            serviceCollection.AddSingleton<IDumpRetrievalService, DefaultDumpRetrievalService>();
            serviceCollection.AddQuartz();
            serviceCollection.AddSingleton<IBackgroundServiceRegistry, BackgroundServiceRegistry>();
            
            serviceCollection.AddSingleton((serviceProvider) => 
            {
                return new DumpRetrievalBackgroundService(serviceProvider);
            });

            return serviceCollection.BuildServiceProvider();
        }

        private static void ConfigureLogging(ServiceCollection serviceCollection, IConfiguration configuration)
        {
            var logConfig = configuration.GetSection("Serilog");
            var logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();

            Log.Logger = logger;
            serviceCollection.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddConfiguration(logConfig);
                builder.AddSerilog(logger, true);
            });
            ServiceProvider = serviceCollection.BuildServiceProvider();
        }
    }
}