using System;
using System.Threading.Tasks;

namespace Ticketbooth.Scanner.Services.Application
{
    public interface IHealthChecker
    {
        event EventHandler<bool> OnAvailabilityChanged;

        bool IsConnected { get; }

        bool IsValid { get; }

        bool IsAvailable { get; }

        Task UpdateNodeHealthAsync();
    }
}
