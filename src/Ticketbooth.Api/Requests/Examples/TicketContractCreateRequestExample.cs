using Swashbuckle.AspNetCore.Examples;
using static TicketContract;

namespace Ticketbooth.Api.Requests.Examples
{
    public class TicketContractCreateRequestExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return new TicketContractCreateRequest
            {
                AccountName = "account 0",
                GasPrice = 100,
                Password = "Hunter2",
                Seats = new Seat[]
                {
                    new Seat
                    {
                        Number = 1,
                        Letter = 'A'
                    },
                    new Seat
                    {
                        Number = 2,
                        Letter = 'A'
                    },
                    new Seat
                    {
                        Number = 3,
                        Letter = 'A'
                    },
                    new Seat
                    {
                        Number = 4,
                        Letter = 'A'
                    },
                    new Seat
                    {
                        Number = 1,
                        Letter = 'B'
                    },
                    new Seat
                    {
                        Number = 2,
                        Letter = 'B'
                    },
                    new Seat
                    {
                        Number = 3,
                        Letter = 'B'
                    },
                    new Seat
                    {
                        Number = 4,
                        Letter = 'B'
                    },
                },
                Sender = "CUtNvY1Jxpn4V4RD1tgphsUKpQdo4q5i54",
                Venue = "Wirral Theatre",
                WalletName = "Wallet One"
            };
        }
    }
}
