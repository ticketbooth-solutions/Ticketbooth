using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace Ticketbooth.Scanner.Services
{
    public class QrCodeValidator
    {
        private readonly NavigationManager _navigationManager;

        public QrCodeValidator(NavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
        }

        [JSInvokable]
        public async Task Validate(string qrCodeResult)
        {
            _navigationManager.NavigateTo("../");
        }
    }
}
