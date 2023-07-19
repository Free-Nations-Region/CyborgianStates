using System;

namespace CyborgianStates.Wrapper
{
    internal class BotEnvironment
    {
        internal virtual void Exit(int exitCode) => Environment.Exit(exitCode);
    }
}
