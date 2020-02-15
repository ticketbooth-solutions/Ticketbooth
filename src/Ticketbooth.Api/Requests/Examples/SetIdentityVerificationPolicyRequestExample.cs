using Swashbuckle.AspNetCore.Examples;

namespace Ticketbooth.Api.Requests.Examples
{
    public class SetIdentityVerificationPolicyRequestExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return new SetIdentityVerificationPolicyRequest
            {
                AccountName = "account 0",
                GasPrice = 100,
                Password = "Hunter2",
                RequireIdentityVerification = true,
                Sender = "CUtNvY1Jxpn4V4RD1tgphsUKpQdo4q5i54",
                WalletName = "Wallet One"
            };
        }
    }
}
