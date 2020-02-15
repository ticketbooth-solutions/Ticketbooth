using Swashbuckle.AspNetCore.Examples;

namespace Ticketbooth.Api.Requests.Examples
{
    public class SetNoReleaseBlocksRequestExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return new SetNoReleaseBlocksRequest
            {
                AccountName = "account 0",
                Count = 5000,
                GasPrice = 100,
                Password = "Hunter2",
                Sender = "CUtNvY1Jxpn4V4RD1tgphsUKpQdo4q5i54",
                WalletName = "Wallet One"
            };
        }
    }
}
