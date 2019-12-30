using System.Threading.Tasks;
using Ticketbooth.Scanner.Eventing;

namespace Ticketbooth.Scanner.Services.Application
{
    public interface IHealthChecker : INotifyPropertyChanged
    {
        bool IsConnected { get; }

        bool IsValid { get; }

        bool IsAvailable { get; }

        string NodeVersion { get; }

        Task UpdateNodeHealthAsync();
    }
}
