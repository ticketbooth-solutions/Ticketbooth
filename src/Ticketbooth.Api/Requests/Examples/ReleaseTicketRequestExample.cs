using Swashbuckle.AspNetCore.Examples;
using static TicketContract;

namespace Ticketbooth.Api.Requests.Examples
{
    public class ReleaseTicketRequestExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return new ReleaseTicketRequest
            {
                AccountName = "account 0",
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
