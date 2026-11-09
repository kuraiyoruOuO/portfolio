# 五關試煉：C# 原型程式

[直接遊玩網頁版](https://kuraiyoruOuO.github.io/portfolio/games/card-battle/)

## 內容與範圍

- `CardBattleUnityDemo.cs`：使用者提供的原型，包含完整戰鬥核心 `CardBattleGame` 與舊版測試介面。
- `CardBattleArtDemo.cs`：角色圖片、普攻／暗器／防禦／大招、受擊效果與 Unity UI 顯示程式。
- `Resources/CardBattleAtlas.png`：依遊戲角色設定生成的 8 欄 × 4 列圖集。
- `Resources/CardBattleFont.otf`：Noto Sans CJK TC 字型的原型所需字元子集，授權見 `Resources/FONT-LICENSE.txt`。

此處是可重用的原型程式與素材，**不是完整 Unity 專案**；不包含使用者電腦上的場景、ProjectSettings 或 package lock。已部署的 Web 匯出位於 `games/card-battle`。

## 在 Unity 6 使用

1. 建立 2D URP 專案，確認安裝 Unity UI 與 Input System。
2. 將此資料夾的兩個 `.cs` 和 `Resources` 複製到專案 Assets；若已有同名原型，更新原檔，避免重複定義類別。
3. 建立空物件，掛上 `CardBattleArtDemo`。原本的核心檔案必須保留；若同一物件有 `CardBattleUnityDemo`，新顯示程式會停用它的舊介面。
4. 按 Play。建議 Game 比例 16:10。

顯示程式優先讀取內附字型，以免 Web 平台無法取得 Windows 系統字型。字型目前只包含原型既有文字與 ASCII；新增文字時需重新建立子集或改用完整且可散布的中文字型。

## 規則

五場連戰，第四場後進入隨機 Boss。HP 跨場延續；前四場勝利後固定回復 10 HP，再額外恢復 15 HP 或選強化。MP 通常每場重設為 15；魔力蓄存使剩餘 MP 保留。強化持續整輪，取得同流派強化後，後續同類選項的出現權重提高。

## 網頁發布

目前的 Web 版由使用者在 Windows Unity 6000.6.8f1 匯出，再針對 GitHub Pages 加入靜態載入頁、gzip 解壓縮與內附中文字型相容處理。戰鬥數值與 WASM 程式邏輯未修改。更新 Web 匯出時，請參考 `games/card-battle/README.md`。

AI 協助：C# 轉寫、Unity 介面、生成角色圖、動畫接線、網頁發布與測試；開發者參與規則設計、Windows Unity 專案設定與實際遊玩測試。
