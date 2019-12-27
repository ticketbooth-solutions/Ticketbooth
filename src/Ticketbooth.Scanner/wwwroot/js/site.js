let scanning = false;

function beginScan(dotnetScanner) {
    if (scanning) {
        StopScanning();
    }

    let scanner = new Instascan.Scanner({ video: document.getElementById('preview'), mirror: false });

    window.addEventListener('popstate', StopScanning);

    Instascan.Camera.getCameras().then(function (cameras) {

        if (cameras.length > 0) {
            scanner.start(cameras[0]);
            scanner.addListener('scan', OnScan);
            dotnetScanner.invokeMethodAsync('NotifyScanStarted');
            scanning = true;
        } else {
            dotnetScanner.invokeMethodAsync('NotifyCameraNotFound');
            console.error('No cameras found.');
        }
    }).catch(function (e) {
        dotnetScanner.invokeMethodAsync('NotifyCameraError');
        console.error(e);
    });

    function OnScan(content) {
        console.log(content);
        dotnetScanner.invokeMethodAsync('Validate', content).then(valid => {
            if (valid) {
                StopScanning();
                window.removeEventListener('popstate', StopScanning);
            }
        });
    }

    function StopScanning() {
        scanning = false;
        scanner.removeListener('scan', OnScan);
        scanner.stop();
    }
}

function replaceIcons() {
    feather.replace({ 'stroke-width': 1.5 });
}