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

        private Url BaseRequest =>
            new Url(_configuration["Stratis:FullNodeApi"])
                .AppendPathSegments("api", "SmartContracts");

        public async Task<Receipt<TValue, object>> FetchReceiptAsync<TValue>(string transactionHash)
        {
            try
            {
                var response = await BaseRequest
                    .AllowHttpStatus(new HttpStatusCode[] { HttpStatusCode.OK, HttpStatusCode.BadRequest })
                    .AppendPathSegment("receipt")
                    .SetQueryParam("txHash", transactionHash)
                    .GetAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var receiptString = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Receipt<TValue, object>>(receiptString);
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

        public async Task<Receipt<object, TLog>[]> FetchReceiptsAsync<TLog>() where TLog : struct
        {
            try
            {
                var response = await BaseRequest
                    .AllowHttpStatus(new HttpStatusCode[] { HttpStatusCode.OK })
                    .AppendPathSegment("receipt-search")
                    .SetQueryParam("contractAddress", _configuration["ContractAddress"])
                    .SetQueryParam("eventName", typeof(TLog).Name)
                    .GetAsync();

                var receiptString = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Receipt<object, TLog>[]>(receiptString);
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
