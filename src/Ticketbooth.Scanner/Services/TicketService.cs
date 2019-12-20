using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Configuration;
using NBitcoin;
using Newtonsoft.Json;
using Stratis.SmartContracts;
using Stratis.SmartContracts.CLR;
using System;
using System.Net;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Data.Dtos;

namespace Ticketbooth.Scanner.Services
{
    public class TicketService : ITicketService
    {
        private readonly IConfiguration _configuration;
        private readonly ISerializer _serializer;
        private readonly Network _network;

        public TicketService(IConfiguration configuration, ISerializer serializer, Network network)
        {
            _configuration = configuration;
            _serializer = serializer;
            _network = network;
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

        public async Task<MethodCallResponse> CheckReservationAsync(TicketContract.Seat seat, Address address)
        {
            var seatIdentifier = _serializer.Serialize(seat);
            var seatIdentifierBytes = BitConverter.ToString(seatIdentifier).Replace("-", string.Empty);
            var base58Address = address.ToUint160().ToBase58Address(_network);

            var response = await BaseRequest
                .WithHeader("Amount", 0)
                .AppendPathSegments("method", "CheckReservation")
                .PostJsonAsync(new { seatIdentifierBytes, address = base58Address });

            if (!response.IsSuccessStatusCode)
            {
                return default;
            }

            var methodCallResponseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<MethodCallResponse>(methodCallResponseString);
        }
    }
}
