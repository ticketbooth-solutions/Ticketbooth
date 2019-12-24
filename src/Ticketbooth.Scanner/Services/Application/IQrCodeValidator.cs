using System.Threading.Tasks;

namespace Ticketbooth.Scanner.Services.Application
{
    public interface IQrCodeValidator
    {
        Task<bool> Validate(string qrCodeData);
    }
}
