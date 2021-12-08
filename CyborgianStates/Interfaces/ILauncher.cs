using System.Threading.Tasks;

namespace CyborgianStates.Interfaces
{
    public interface ILauncher
    {
        bool IsRunning { get; }

        Task RunAsync();
        Task ShutdownAsync();
    }
}