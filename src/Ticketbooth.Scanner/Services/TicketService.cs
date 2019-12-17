using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Stratis.SmartContracts;
using System.Net;
using System.Threading.Tasks;

namespace Ticketbooth.Scanner.Services
{
    public class TicketService : ITicketService
    {
        private readonly IConfiguration _configuration;
        private readonly ISerializer _serializer;

        public TicketService(IConfiguration configuration, ISerializer serializer)
        {
            _configuration = configuration;
            _serializer = serializer;
        }

        private IFlurlRequest BaseRequest =>
            new Url(_configuration["Stratis:FullNodeApi"])
                .AppendPathSegments("api", "contract", _configuration["ContractAddress"])
                .AllowHttpStatus(new HttpStatusCode[] { HttpStatusCode.OK, HttpStatusCode.BadRequest })
                .WithHeader("GasPrice", _configuration["Stratis:GasPrice"])
                .WithHeader("GasLimit", _configuration["Stratis:GasLimit"])
                .WithHeader("FeeAmount", _configuration["Stratis:Fee"])
                .WithHeader("WalletName", _configuration["Stratis:Wallet"])
                .WithHeader("WalletPassword", _configuration["Stratis:Password"])
                .WithHeader("Sender", _configuration["Stratis:Address"]);

        public async Task<TicketContract.ReservationQueryResult> CheckReservation(TicketContract.Seat seat, Address address)
        {
            var seatIdentifierBytes = _serializer.Serialize(seat);
            var response = await BaseRequest
                .WithHeader("Amount", 0)
                .AppendPathSegments("method", "CheckReservation")
                .PostJsonAsync(new { seatIdentifierBytes, address });

            if (!response.IsSuccessStatusCode)
            {
                return default;
            }

            var reservationQueryResultString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TicketContract.ReservationQueryResult>(reservationQueryResultString);
        }
    }
}
