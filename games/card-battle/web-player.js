"use strict";
(() => {
  const canvas = document.getElementById("unity-canvas");
  const overlay = document.getElementById("loading");
  const start = document.getElementById("start");
  const status = document.getElementById("status");
  const progress = document.getElementById("progress");
  const warning = document.getElementById("warning");
  const fullscreen = document.getElementById("fullscreen");
  const objectURLs = [];
  let instance = null;
  let starting = false;

  // Pages does not supply Unity compression headers. Inflate explicitly.
  async function loadAsset(path, type) {
    const response = await fetch(path);
    if (!response.ok) throw new Error(`下載失敗（${response.status}）：${path}`);
    let data = await response.arrayBuffer();
    const magic = new Uint8Array(data, 0, Math.min(2, data.byteLength));
    if (magic[0] === 0x1f && magic[1] === 0x8b) {
      if (!("DecompressionStream" in window)) {
        throw new Error("請更新 Chrome、Edge、Firefox 或 Safari，以支援遊戲解壓縮。");
      }
      const stream = new Blob([data]).stream().pipeThrough(new DecompressionStream("gzip"));
      data = await new Response(stream).arrayBuffer();
    }
    const url = URL.createObjectURL(new Blob([data], { type }));
    objectURLs.push(url);
    return url;
  }
  function releaseURLs() {
    objectURLs.splice(0).forEach(url => URL.revokeObjectURL(url));
  }
  start.addEventListener("click", async () => {
    if (starting || instance) return;
    starting = true;
    start.disabled = true;
    start.textContent = "正在準備…";
    progress.hidden = false;
    warning.textContent = "";
    status.textContent = "正在下載與解壓縮遊戲資料…";
    try {
      let completed = 0;
      const specs = [
        ["Build/CardBattleWeb.data.gz", "application/octet-stream"],
        ["Build/CardBattleWeb.framework.js.gz", "text/javascript"],
        ["Build/CardBattleWeb.wasm.gz", "application/wasm"]
      ];
      const urls = await Promise.all(specs.map(async ([path, type]) => {
        const url = await loadAsset(path, type);
        progress.value = ++completed / 3 * 0.45;
        return url;
      }));
      status.textContent = "正在啟動 Unity 引擎…";
      instance = await createUnityInstance(canvas, {
        dataUrl: urls[0], frameworkUrl: urls[1], codeUrl: urls[2],
        streamingAssetsUrl: "StreamingAssets",
        companyName: "DefaultCompany", productName: "FiveTrialsCardBattle", productVersion: "1.0",
        fullscreenElementID: "game-frame",
        devicePixelRatio: Math.min(window.devicePixelRatio || 1, 2),
        showBanner(message, type) {
          if (type === "error" || type === "warning") warning.textContent = message;
        }
      }, value => { progress.value = 0.45 + value * 0.55; });
      window.cardBattleInstance = instance;
      overlay.hidden = true;
      fullscreen.disabled = false;
      canvas.focus();
      releaseURLs();
    } catch (error) {
      releaseURLs();
      console.error("Card battle startup failed:", error);
      status.textContent = "遊戲尚未成功載入。";
      warning.textContent = String(error.message || error);
      start.textContent = "重新載入";
      start.disabled = false;
      start.onclick = () => window.location.reload();
    }
  });
  fullscreen.addEventListener("click", () => {
    if (instance) instance.SetFullscreen(1);
  });
})();
