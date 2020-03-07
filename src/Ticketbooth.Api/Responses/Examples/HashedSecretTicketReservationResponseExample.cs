using Swashbuckle.AspNetCore.Examples;

namespace Ticketbooth.Api.Responses.Examples
{
    public class HashedSecretTicketReservationResponseExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return new HashedSecretTicketReservationResponse
            {
                TransactionHash = "c80b5075fcd6d0d58e235b86556834d7dc71be48e8ee5940071431963b9f47d6",
                Secret = "h8pqqc2_r90je7v",
                CustomerName = new CbcValues
                {
                    Key = new byte[32] { 8, 4, 54, 28, 222, 198, 82, 72, 58, 72, 118, 173, 183, 185, 109, 93, 3, 32, 4, 37, 193, 181, 188, 93, 7, 1, 39, 37, 242, 255, 2, 12 },
                    IV = new byte[16] { 73, 71, 8, 191, 38, 32, 235, 28, 2, 10, 13, 39, 112, 32, 31, 9 }
                }
            };
        }
    }
}
