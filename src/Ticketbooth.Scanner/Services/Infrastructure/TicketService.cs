using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Stratis.SmartContracts;
using System;
using System.Net;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Data.Dtos;

namespace Ticketbooth.Scanner.Services.Infrastructure
{
    public class TicketService : ITicketService
    {
        private readonly ILogger<TicketService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ISerializer _serializer;

        public TicketService(ILogger<TicketService> logger, IConfiguration configuration, ISerializer serializer)
        {
            _logger = logger;
            _configuration = configuration;
            _serializer = serializer;
        }

        private IFlurlRequest BaseRequest =>
            new Url(_configuration["Stratis:FullNodeApi"])
                .AppendPathSegments("api", "contract", _configuration["ContractAddress"])
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

            try
            {
                var response = await BaseRequest
                    .AppendPathSegments("method", "CheckReservation")
                    .AllowHttpStatus(new HttpStatusCode[] { HttpStatusCode.OK })
                    .WithHeader("Amount", 0)
                    .PostJsonAsync(new { seatIdentifierBytes, address });

                var methodCallResponseString = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<MethodCallResponse>(methodCallResponseString);
            }
            catch (FlurlHttpException e)
            {
                _logger.LogError(e.Message);
                return null;
            }
            catch (JsonException e)
            {
                _logger.LogError(e.Message);
                return null;
            }
        }
    }
}
