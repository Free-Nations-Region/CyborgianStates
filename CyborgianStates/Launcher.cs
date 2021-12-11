using CyborgianStates.Interfaces;
using CyborgianStates.Wrapper;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace CyborgianStates
{
    public class Launcher : ILauncher
    {
        private IBotService _botService;
        public bool IsRunning { get; private set; }
        private BotEnvironment _env;
        public Launcher() => _env = new BotEnvironment();
        public async Task RunAsync()
        {
            IsRunning = true;
            _botService = Program.ServiceProvider.GetService<IBotService>();
            await _botService.InitAsync().ConfigureAwait(false);
            await _botService.RunAsync().ConfigureAwait(false);
        }
        public async Task ShutdownAsync()
        {
            await _botService.ShutdownAsync().ConfigureAwait(false);
            Exit(0);
        }

        internal void SetEnv(BotEnvironment botEnvironment) => _env = botEnvironment;
        internal virtual void Exit(int exitCode) => _env.Exit(exitCode);
    }
}