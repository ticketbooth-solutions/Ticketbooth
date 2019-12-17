using Stratis.SmartContracts;
using System.Threading.Tasks;
using static TicketContract;

namespace Ticketbooth.Scanner.Services
{
    public interface ITicketService
    {
        Task<ReservationQueryResult> CheckReservation(Seat seat, Address address);
    }
}
