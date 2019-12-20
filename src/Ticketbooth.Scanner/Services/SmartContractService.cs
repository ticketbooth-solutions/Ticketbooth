using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Data.Dtos;

namespace Ticketbooth.Scanner.Services
{
    public class SmartContractService : ISmartContractService
    {

        private readonly IConfiguration _configuration;

        public SmartContractService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private IFlurlRequest BaseRequest =>
            new Url(_configuration["Stratis:FullNodeApi"])
                .AppendPathSegments("api", "SmartContracts")
                .AllowHttpStatus(new HttpStatusCode[] { HttpStatusCode.OK, HttpStatusCode.BadRequest });

        public async Task<Receipt<T>> FetchReceiptAsync<T>(string transactionHash)
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
    }
}
