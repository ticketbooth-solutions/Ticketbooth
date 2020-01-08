using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace Ticketbooth.Scanner.Services.Infrastructure
{
    public class QrCodeScanner : IQrCodeScanner
    {
        private readonly IJSRuntime _jsRuntime;

        public QrCodeScanner(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public Func<string, Task> Validation { get; set; }

        public Action ScanStarted { get; set; }

        public Action CameraNotFound { get; set; }

        public Action CameraError { get; set; }

        public ValueTask Start()
        {
            return _jsRuntime.InvokeVoidAsync("beginScan", DotNetObjectReference.Create(this));
        }

        [JSInvokable]
        public async Task Validate(string qrCodeResult)
        {
            await Validation.Invoke(qrCodeResult);
        }

        [JSInvokable]
        public Task NotifyScanStarted()
        {
            ScanStarted.Invoke();
            return Task.CompletedTask;
        }

        [JSInvokable]
        public Task NotifyCameraNotFound()
        {
            CameraNotFound.Invoke();
            return Task.CompletedTask;
        }

        [JSInvokable]
        public Task NotifyCameraError()
        {
            CameraError.Invoke();
            return Task.CompletedTask;
        }
    }
}
