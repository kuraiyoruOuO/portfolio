# 五關試煉 Web Player

Play: https://kuraiyoruOuO.github.io/portfolio/games/card-battle/

## Hosting

Static GitHub Pages deployment, no server or account needed to play. `index.html` and `web-player.js` load the user's Unity 6000.6.8f1 Web build. Binary code and game rules are retained from the uploaded build.

The uploaded Brotli-compressed Unity files were decoded and recompressed as deterministic gzip. GitHub Pages cannot configure Unity-specific Content-Encoding headers, so `web-player.js` decompresses them with the browser's native `DecompressionStream` before passing typed blob URLs to Unity. No external decompression library or CDN is required. A header-aware check also accepts assets transparently decompressed by a future server.

## Chinese font compatibility

The original C# presentation created a font from Windows OS names, which are unavailable in Web builds. The UnityWebData package's `Resources/unity default resources` font asset named `LegacyRuntime` was updated with an embedded Noto Sans CJK TC subset and adjusted line spacing. Game code/WASM, balance, portraits, scenes and rules were not changed. The subset contains the Chinese text used by the provided C# prototype plus printable ASCII. Font source: https://github.com/notofonts/noto-cjk ; SIL OFL 1.1 text is in `FONT-LICENSE.txt`.

The reusable C# presentation in `/unity-source` loads the bundled font first. For future builds, include an appropriately licensed Chinese font in Assets/Resources and load it explicitly rather than using OS font lookup, so the build does not require the resource compatibility patch.

## Updating

Export the complete current Unity scene to Web, include the font in the Unity project, then replace all build files together and update loader paths. If using this page's existing static loading adapter, gzip the uncompressed `.data`, `.framework.js` and `.wasm` into the matching `Build/CardBattleWeb.*.gz` files. Never serve new code with stale data or mismatched assets. Test startup, visible Chinese, an action, rest/restart, and browser errors over HTTP(S). `file://` double-click does not run Unity builds reliably.

## Verification

Tested in Chromium with WebGL 2 via software rendering: loader startup, visible Chinese UI and portraits, combat interaction and restart. The browser reported unsupported/stripped optional URP post-processing shaders; these did not block the 2D UI or combat.

Current resources are approximately 17.5 MB compressed. Use an up-to-date desktop Chrome, Edge or Firefox with WebGL 2 and DecompressionStream support. Mobile browsers may have memory and fullscreen limitations.
