using System.Threading.Tasks;
using Ticketbooth.Scanner.Data.Dtos;

namespace Ticketbooth.Scanner.Services.Infrastructure
{
    public interface ISmartContractService
    {
        Task<Receipt<TValue, object>> FetchReceiptAsync<TValue>(string transactionHash);

        Task<Receipt<object, TLog>[]> FetchReceiptsAsync<TLog>() where TLog : struct;
    }
}
