using System.Threading.Tasks;
using Ticketbooth.Scanner.Data.Dtos;

namespace Ticketbooth.Scanner.Services.Infrastructure
{
    public interface ISmartContractService
    {
        Task<Receipt<T>> FetchReceiptAsync<T>(string transactionHash);
    }
}
