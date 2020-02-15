using Swashbuckle.AspNetCore.Examples;

namespace Ticketbooth.Api.Responses.Examples
{
    public class TicketReservationDetailsResponseExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return new
            {
                TransactionHash = "dddceacac7b11409e024013aa688c60dae40b3a2281e4fbf169bb0b454241d55",
                Secret = new
                {
                    Cipher = new byte[16] { 43, 82, 82, 17, 111, 209, 233, 18, 3, 28, 181, 28, 87, 9, 44, 2 },
                    Key = new byte[32] { 98, 182, 82, 74, 8, 2, 43, 29, 28, 109, 248, 175, 184, 72, 71, 72, 90, 211, 24, 129, 182, 174, 7, 88, 98, 104, 19, 28, 82, 72, 33, 190 },
                    IV = new byte[16] { 82, 74, 27, 110, 129, 18, 231, 248, 27, 90, 8, 22, 123, 8, 7, 21 }
                },
                CustomerName = new
                {
                    Cipher = new byte[16] { 90, 82, 82, 73, 74, 110, 201, 28, 27, 5, 29, 188, 28, 83, 7, 83 },
                    Key = new byte[32] { 8, 4, 54, 28, 222, 198, 82, 72, 58, 72, 118, 173, 183, 185, 109, 93, 3, 32, 4, 37, 193, 181, 188, 93, 7, 1, 39, 37, 242, 255, 2, 12 },
                    IV = new byte[16] { 73, 71, 8, 191, 38, 32, 235, 28, 2, 10, 13, 39, 112, 32, 31, 9 }
                }
            };
        }
    }
}
