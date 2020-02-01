using Swashbuckle.AspNetCore.Examples;
using static TicketContract;

namespace Ticketbooth.Api.Requests.Examples
{
    public class ReserveTicketRequestExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return new ReserveTicketRequest
            {
                AccountName = "account 0",
                CustomerName = "Benjamin Swift",
                GasPrice = 100,
                Password = "Hunter2",
                Seat = new Seat
                {
                    Number = 3,
                    Letter = 'D'
                },
                Sender = "CUtNvY1Jxpn4V4RD1tgphsUKpQdo4q5i54",
                WalletName = "Wallet One"
            };
        }
    }
}
