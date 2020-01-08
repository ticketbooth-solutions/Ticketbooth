using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Data.Dtos;

namespace Ticketbooth.Scanner.Services.Infrastructure
{
    public class SmartContractService : ISmartContractService
    {

        private readonly IConfiguration _configuration;
        private readonly ILogger<SmartContractService> _logger;

        public SmartContractService(IConfiguration configuration, ILogger<SmartContractService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private IFlurlRequest BaseRequest =>
            new Url(_configuration["Stratis:FullNodeApi"])
                .AppendPathSegments("api", "SmartContracts")
                .AllowHttpStatus(new HttpStatusCode[] { HttpStatusCode.OK, HttpStatusCode.BadRequest });

        public async Task<Receipt<T>> FetchReceiptAsync<T>(string transactionHash)
        {
            try
            {
                var response = await BaseRequest
                    .AppendPathSegment("receipt")
                    .SetQueryParam("txHash", transactionHash)
                    .GetAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var receiptString = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Receipt<T>>(receiptString);
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
