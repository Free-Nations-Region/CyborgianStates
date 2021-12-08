using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyborgianStates.Interfaces
{
    public interface IBackgroundServiceRegistry
    {
        void Register(IBackgroundService service);
        Task StartAsync();
        Task ShutdownAsync();
    }
}
