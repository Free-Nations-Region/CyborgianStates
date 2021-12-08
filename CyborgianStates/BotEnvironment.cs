using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyborgianStates
{
    internal class BotEnvironment
    {
        internal virtual void Exit(int exitCode)
        {
            Environment.Exit(exitCode);
        }
    }
}
