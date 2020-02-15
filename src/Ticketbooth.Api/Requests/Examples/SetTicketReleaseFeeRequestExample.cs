using Swashbuckle.AspNetCore.Examples;

namespace Ticketbooth.Api.Requests.Examples
{
    public class SetTicketReleaseFeeRequestExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return new SetTicketReleaseFeeRequest
            {
                AccountName = "account 0",
                Fee = 100000000,
                GasPrice = 100,
                Password = "Hunter2",
                Sender = "CUtNvY1Jxpn4V4RD1tgphsUKpQdo4q5i54",
                WalletName = "Wallet One"
            };
        }
    }
}
