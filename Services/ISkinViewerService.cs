namespace MClauncher.Services
{
    public interface ISkinViewerService
    {
        string GenerateHtml(string username, string skinUrl, string fallbackUrl);
    }

    public class SkinViewerService : ISkinViewerService
    {
        public string GenerateHtml(string username, string skinUrl, string fallbackUrl)
        {
            // skinUrl and fallbackUrl come from minotar.net
            // We use skinview3d loaded via jsDelivr CDN (more reliable) + fallback to 2D
            return $$"""
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <meta http-equiv="Content-Security-Policy" content="default-src * 'unsafe-inline' 'unsafe-eval' data: blob:;">
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        html, body {
            width: 100%; height: 100%; overflow: hidden;
            background: linear-gradient(160deg, #0a0e1a 0%, #0d1b2e 40%, #0a1a10 100%);
            font-family: 'Segoe UI', sans-serif;
        }
        #app {
            width: 100vw; height: 100vh;
            display: flex; flex-direction: column;
            align-items: center; justify-content: center;
            position: relative;
        }
        /* Ambient glow */
        .glow-left {
            position: absolute; top: 50%; left: -80px;
            width: 300px; height: 300px;
            background: radial-gradient(circle, rgba(98,239,173,0.08), transparent 70%);
            transform: translateY(-50%); pointer-events: none;
        }
        .glow-right {
            position: absolute; top: 30%; right: -80px;
            width: 250px; height: 250px;
            background: radial-gradient(circle, rgba(0,120,212,0.06), transparent 70%);
            pointer-events: none;
        }
        /* Canvas wrapper */
        #canvas-wrap {
            flex: 1; width: 100%; display: flex;
            align-items: center; justify-content: center;
        }
        canvas { display: block; }
        /* Info bar */
        .info-bar {
            width: 100%; padding: 12px 20px;
            background: rgba(10,14,26,0.9);
            border-top: 1px solid rgba(98,239,173,0.15);
            display: flex; align-items: center; justify-content: space-between;
        }
        .player-name {
            font-size: 14px; font-weight: 700; color: #62EFAD;
            text-shadow: 0 0 12px rgba(98,239,173,0.5);
        }
        .controls-hint {
            font-size: 11px; color: #445566;
        }
        /* Fallback / loading states */
        .fallback-box {
            display: flex; flex-direction: column;
            align-items: center; justify-content: center;
            gap: 12px;
        }
        .fallback-box img {
            width: 180px; height: 180px;
            border-radius: 12px;
            border: 2px solid rgba(98,239,173,0.3);
            image-rendering: pixelated;
            box-shadow: 0 4px 32px rgba(0,180,216,0.25);
        }
        .fallback-box .label { color: #62EFAD; font-size: 13px; font-weight: 600; }
        .fallback-box .sub { color: #445566; font-size: 11px; text-align: center; max-width: 220px; }
        #loading-text { color: #445566; font-size: 12px; }
        @keyframes spin {
            from { transform: rotate(0deg); } to { transform: rotate(360deg); }
        }
        .spinner {
            width: 32px; height: 32px;
            border: 3px solid #1a2a3a;
            border-top-color: #62EFAD;
            border-radius: 50%;
            animation: spin 0.8s linear infinite;
        }
    </style>
</head>
<body>
<div id="app">
    <div class="glow-left"></div>
    <div class="glow-right"></div>

    <div id="canvas-wrap">
        <!-- Spinner shown while loading -->
        <div id="loading-state" class="fallback-box">
            <div class="spinner"></div>
            <span id="loading-text">Loading 3D viewer...</span>
        </div>
    </div>

    <div class="info-bar">
        <span class="player-name">🎮 {{username}}</span>
        <span class="controls-hint">🖱️ Drag → rotate &nbsp;|&nbsp; Scroll → zoom</span>
    </div>
</div>

<!-- Load skinview3d from jsDelivr (more reliable than unpkg in restricted environments) -->
<script>
(function() {
    var SKIN_URL     = '{{skinUrl}}';
    var FALLBACK_URL = '{{fallbackUrl}}';
    var USERNAME     = '{{username}}';

    function showFallback(reason) {
        var wrap = document.getElementById('canvas-wrap');
        wrap.innerHTML =
            '<div class="fallback-box">' +
            '<img src="' + FALLBACK_URL + '" alt="' + USERNAME + '" />' +
            '<span class="label">' + USERNAME + '</span>' +
            '<span class="sub">' + reason + '</span>' +
            '</div>';
    }

    function startViewer() {
        var wrap = document.getElementById('canvas-wrap');

        // Check library loaded
        if (!window.skinview3d) {
            showFallback('3D library unavailable — showing 2D avatar');
            return;
        }

        try {
            // Clear loading spinner
            wrap.innerHTML = '';

            var w = wrap.clientWidth  || 400;
            var h = wrap.clientHeight || 380;

            var viewer = new skinview3d.SkinViewer({
                width: w,
                height: h,
                skin: SKIN_URL,
                enableControls: true
            });

            // Style canvas
            viewer.canvas.style.borderRadius = '0';
            wrap.appendChild(viewer.canvas);

            // Walking animation
            viewer.animation = new skinview3d.WalkingAnimation();
            viewer.animation.speed = 0.6;
            viewer.autoRotate = true;
            viewer.autoRotateSpeed = 0.6;
            viewer.camera.position.z = 70;

            // Try to load cape (silently fail)
            try { viewer.loadCape('https://minotar.net/cape/' + USERNAME); } catch(e) {}

            // Resize
            window.addEventListener('resize', function() {
                var nw = wrap.clientWidth  || 400;
                var nh = wrap.clientHeight || 380;
                viewer.setSize(nw, nh);
            });

            // Mouse rotate (stops auto-rotate while dragging)
            var dragging = false, lastX = 0;
            viewer.canvas.addEventListener('mousedown', function(e) {
                dragging = true; lastX = e.clientX;
                viewer.autoRotate = false;
            });
            viewer.canvas.addEventListener('mousemove', function(e) {
                if (!dragging) return;
                viewer.playerWrapper.rotation.y += (e.clientX - lastX) * 0.01;
                lastX = e.clientX;
            });
            viewer.canvas.addEventListener('mouseup',    function() { dragging = false; viewer.autoRotate = true; });
            viewer.canvas.addEventListener('mouseleave', function() { dragging = false; viewer.autoRotate = true; });

            // Zoom with scroll
            viewer.canvas.addEventListener('wheel', function(e) {
                e.preventDefault();
                viewer.camera.position.z = Math.max(30, Math.min(150,
                    viewer.camera.position.z + (e.deltaY > 0 ? 4 : -4)));
            }, { passive: false });

        } catch(err) {
            console.error('Viewer init error:', err);
            showFallback('Could not initialize 3D viewer');
        }
    }

    // Dynamically load skinview3d library
    function loadLib(urls, index, onDone) {
        if (index >= urls.length) { onDone(false); return; }
        var s = document.createElement('script');
        s.src = urls[index];
        s.onload  = function() { onDone(true); };
        s.onerror = function() { loadLib(urls, index + 1, onDone); };
        document.head.appendChild(s);
    }

    // Try multiple CDN sources for reliability
    var CDN_URLS = [
        'https://cdn.jsdelivr.net/npm/skinview3d@3.2.0/bundles/skinview3d.bundle.js',
        'https://unpkg.com/skinview3d@3.2.0/bundles/skinview3d.bundle.js',
        'https://cdn.skypack.dev/skinview3d@3.2.0/bundles/skinview3d.bundle.js'
    ];

    document.getElementById('loading-text').textContent = 'Loading skin library...';

    loadLib(CDN_URLS, 0, function(success) {
        if (success) {
            document.getElementById('loading-text').textContent = 'Rendering skin...';
            // Small delay to ensure library is fully initialised
            setTimeout(startViewer, 100);
        } else {
            showFallback('Could not load 3D library — showing 2D avatar');
        }
    });
})();
</script>
</body>
</html>
""";
        }
    }
}
