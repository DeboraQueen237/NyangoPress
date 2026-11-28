window.nyangoPress = {
    downloadFile: function (fileName, contentType, base64Data) {
        const link = document.createElement('a');
        link.href = 'data:' + contentType + ';base64,' + base64Data;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    },

    printPage: function () {
        window.print();
    },

    signaturePad: {
        instances: {},
        init: function (canvasId) {
            const canvas = document.getElementById(canvasId);
            if (!canvas) return;
            const ctx = canvas.getContext('2d');
            canvas.width = canvas.offsetWidth || 420;
            canvas.height = canvas.offsetHeight || 170;
            ctx.lineWidth = 2.5;
            ctx.lineCap = 'round';
            ctx.strokeStyle = '#0B3C5D';

            let drawing = false;
            let hasDrawn = false;

            const getPos = (e) => {
                const rect = canvas.getBoundingClientRect();
                const clientX = e.touches ? e.touches[0].clientX : e.clientX;
                const clientY = e.touches ? e.touches[0].clientY : e.clientY;
                return { x: clientX - rect.left, y: clientY - rect.top };
            };

            const start = (e) => {
                e.preventDefault();
                drawing = true;
                hasDrawn = true;
                const pos = getPos(e);
                ctx.beginPath();
                ctx.moveTo(pos.x, pos.y);
            };
            const move = (e) => {
                if (!drawing) return;
                e.preventDefault();
                const pos = getPos(e);
                ctx.lineTo(pos.x, pos.y);
                ctx.stroke();
            };
            const end = () => { drawing = false; };

            canvas.onmousedown = start;
            canvas.onmousemove = move;
            window.addEventListener('mouseup', end);
            canvas.ontouchstart = start;
            canvas.ontouchmove = move;
            canvas.ontouchend = end;

            this.instances[canvasId] = { canvas, ctx, isDrawn: () => hasDrawn, resetDrawn: () => { hasDrawn = false; } };
        },
        clear: function (canvasId) {
            const inst = this.instances[canvasId];
            if (!inst) return;
            inst.ctx.clearRect(0, 0, inst.canvas.width, inst.canvas.height);
            inst.resetDrawn();
        },
        getDataUrl: function (canvasId) {
            const inst = this.instances[canvasId];
            if (!inst || !inst.isDrawn()) return null;
            return inst.canvas.toDataURL('image/png');
        }
    },

    camera: {
        stream: null,
        start: async function (videoId) {
            const video = document.getElementById(videoId);
            if (!video || !navigator.mediaDevices) return false;
            try {
                this.stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'environment' } });
                video.srcObject = this.stream;
                await video.play();
                return true;
            } catch {
                return false;
            }
        },
        capture: function (videoId) {
            const video = document.getElementById(videoId);
            if (!video || !video.videoWidth) return null;
            const canvas = document.createElement('canvas');
            const scale = Math.min(1, 900 / video.videoWidth);
            canvas.width = video.videoWidth * scale;
            canvas.height = video.videoHeight * scale;
            canvas.getContext('2d').drawImage(video, 0, 0, canvas.width, canvas.height);
            return canvas.toDataURL('image/jpeg', 0.82);
        },
        stop: function () {
            if (this.stream) {
                this.stream.getTracks().forEach(t => t.stop());
                this.stream = null;
            }
        }
    },

    qrScanner: {
        intervalId: null,
        start: async function (videoId, dotNetHelper) {
            const ok = await window.nyangoPress.camera.start(videoId);
            if (!ok) return false;
            if ('BarcodeDetector' in window) {
                const detector = new BarcodeDetector({ formats: ['qr_code'] });
                const video = document.getElementById(videoId);
                this.intervalId = setInterval(async () => {
                    if (!video || video.readyState < 2) return;
                    try {
                        const codes = await detector.detect(video);
                        if (codes && codes.length > 0) {
                            await dotNetHelper.invokeMethodAsync('OnQrDetectedFromJs', codes[0].rawValue);
                        }
                    } catch { }
                }, 600);
            }
            return true;
        },
        stop: function () {
            if (this.intervalId) {
                clearInterval(this.intervalId);
                this.intervalId = null;
            }
            window.nyangoPress.camera.stop();
        }
    },

    geolocation: {
        getCurrentPosition: function () {
            return new Promise((resolve) => {
                if (!navigator.geolocation) {
                    resolve({ latitude: 4.0511, longitude: 9.7679, accuracy: 25 });
                    return;
                }
                navigator.geolocation.getCurrentPosition(
                    (pos) => resolve({
                        latitude: pos.coords.latitude,
                        longitude: pos.coords.longitude,
                        accuracy: pos.coords.accuracy
                    }),
                    () => resolve({ latitude: 4.0511, longitude: 9.7679, accuracy: 25 }),
                    { enableHighAccuracy: true, timeout: 4000 }
                );
            });
        }
    },

    map: {
        instances: {},
        render: function (containerId, driverLat, driverLng, destLat, destLng, label) {
            const el = document.getElementById(containerId);
            if (!el) return;
            if (typeof L === 'undefined') {
                el.innerHTML = `<div style="height:100%;display:flex;flex-direction:column;align-items:center;justify-content:center;background:linear-gradient(135deg,#0B3C5D12,#19B5C618);padding:16px;text-align:center;">
                    <div style="font-weight:700;color:#0B3C5D;margin-bottom:6px;">📍 ${label || 'Position GPS en direct'}</div>
                    <div style="font-size:13px;color:#475569;">Livreur : ${(driverLat || 4.0511).toFixed(4)}, ${(driverLng || 9.7679).toFixed(4)}</div>
                    ${destLat ? `<div style="font-size:12px;color:#64748B;margin-top:4px;">Destination : ${destLat.toFixed(4)}, ${destLng.toFixed(4)}</div>` : ''}
                </div>`;
                return;
            }

            let map = this.instances[containerId];
            if (map) {
                map.remove();
            }
            const centerLat = driverLat || destLat || 4.0511;
            const centerLng = driverLng || destLng || 9.7679;
            map = L.map(containerId).setView([centerLat, centerLng], 13);
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '&copy; OpenStreetMap'
            }).addTo(map);

            if (driverLat && driverLng) {
                L.marker([driverLat, driverLng]).addTo(map).bindPopup(label || 'Livreur NyangoPress').openPopup();
            }
            if (destLat && destLng) {
                L.circleMarker([destLat, destLng], { radius: 9, color: '#0B3C5D', fillColor: '#F4B942', fillOpacity: 0.9 })
                    .addTo(map).bindPopup('Destination client');
            }
            this.instances[containerId] = map;
        }
    }
};
