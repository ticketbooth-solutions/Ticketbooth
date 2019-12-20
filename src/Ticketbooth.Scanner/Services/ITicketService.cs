using Stratis.SmartContracts;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Data.Dtos;
using static TicketContract;

namespace Ticketbooth.Scanner.Services
{
    public interface ITicketService
    {
        Task<MethodCallResponse> CheckReservationAsync(Seat seat, Address address);
    }
}
