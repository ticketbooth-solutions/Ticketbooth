(function (history) {
    var pushState = history.pushState;
    history.pushState = function (state) {
        if (typeof history.onpushstate === "function") {
            history.onpushstate({ state: state });
        }
        window.dispatchEvent(new Event('popstate'));
        return pushState.apply(history, arguments);
    };
})(window.history);

async function beginScan(dotnetScanner) {
    let scanning = true;

    async function StopScanning() {
        scanning = false;
        scanner.removeListener('scan', OnScan);
        window.removeEventListener('pushstate', StopScanning);
        window.removeEventListener('popstate', StopScanning);
        await scanner.stop();
    }

    async function OnScan(content) {
        console.log(content);
        await dotnetScanner.invokeMethodAsync('Validate', content);
        await StopScanning();
    }

    let scanner = new Instascan.Scanner({ video: document.getElementById('preview'), mirror: false });
    window.addEventListener('popstate', StopScanning);

    try {
        let cameras = await Instascan.Camera.getCameras();

        if (cameras.length > 0) {
            scanner.addListener('scan', await OnScan);
            await scanner.start(cameras[0]);
            await dotnetScanner.invokeMethodAsync('NotifyScanStarted');
        } else {
            await dotnetScanner.invokeMethodAsync('NotifyCameraNotFound');
            console.error('No cameras found.');
        }
    }
    catch(e) {
        await dotnetScanner.invokeMethodAsync('NotifyCameraError');
        console.error(e);
    }
    finally {
        if (scanning === false) {
            await StopScanning();
        }
    }
}