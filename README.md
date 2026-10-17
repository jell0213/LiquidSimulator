# Liquid Lab / 液體模擬器

Unity **6000.6.0f1** · **Universal 2D (URP)** · 滑鼠互動桌面小遊戲。

## 開啟與遊玩

1. 在 Unity Hub 安裝 **6000.6.0f1**，使用 Add project from disk 選取本資料夾。
2. 等待 Package Manager 匯入依賴、C# 編譯完成。首次匯入會建立 `Assets/Scenes/LiquidSandbox.unity`、URP Asset 與 **2D Renderer**，並將場景加入 Build Settings。
3. 開啟該場景，按 Play。如果場景未自動產生，使用選單 **Liquid Lab → Open sandbox in 2D**（既有場景請直接從 Project 視窗開啟）。
4. 按住容器內部或邊框拖曳，左右來回晃動可產生波浪。按 **Q / E** 傾斜；拖曳時也能用滾輪傾斜，角度限制 ±65°。
5. Canvas 的 **Viscosity** 滑桿可從 Water 調到 Syrup；**Pause / Play** 暫停，**Reset** 重新填充並讓容器回到中央。

專案使用內建 Input Manager；首次設定會選擇此輸入後端。若手動變更為僅使用新版 Input System，請在 Player Settings 將 Active Input Handling 改為 Input Manager (Old) 或 Both，並依 Unity 提示重新啟動。

## 匯入後看到 3D Scene 視窗？

Scene 視窗的 2D/3D 按鈕只控制編輯器檢視，並不決定使用哪個渲染管線。專案會設定 URP 的 2D Renderer，並在新建場景中建立 Orthographic 相機。

若已有舊版自動產生的場景，更新程式後使用 **Liquid Lab → Open sandbox in 2D**，依提示保留目前未儲存的工作。此功能會開啟遊戲場景、補上／設定正交 Main Camera，並切換 Scene 視窗為 2D。按 **Ctrl/Cmd+S** 儲存場景。容器、液體與 Canvas 在 **Play** 後由程式產生，編輯模式只看到相機屬於正常行為。

## 液體使用什麼物件？

液體使用 **GameObject + MeshFilter + MeshRenderer**，搭配自訂 CPU 粒子流體求解器。Unity 內建 Particle System 適合噴濺特效；大量互相碰撞的 Rigidbody2D 圓球則更接近顆粒，兩者都不是完整的液體求解器。因此本專案以 551 個質點進行 double-density relaxation，將密度場以 marching triangles 生成連續動態網格，而不是顯示一堆球。

- 固定 120 Hz 子步進，壓力／近鄰密度位置約束、世界座標重力、成對黏性速度擴散。
- 空間雜湊近鄰查詢，移動與旋轉容器的局部空間邊界約束、接觸速度和壁面拖曳。
- 容器移動保留液體慣性，傾斜時重力仍朝世界下方。
- URP 2D Unlit 連續液面與深淺漸層；液面不需要額外素材或付費外掛。

這是重視互動手感的二維近似，不是工程級 Navier–Stokes 求解器。黏稠度為無因次控制值，Water／Oil／Syrup 是手感標籤，不代表真實物質量測參數。方形容器四面封閉，因此液體不會倒出；沒有三維體積、折射或 GPU 流體模擬。高速移動受限，以避免離散碰撞穿透。低幀率時限制追趕時間，可能出現模擬慢動作。

## 檔案

- `Assets/Scripts/LiquidSimulation.cs`：流體、碰撞與黏度。
- `Assets/Scripts/LiquidSurface.cs`：密度場與連續液面網格。
- `Assets/Scripts/LiquidSandbox.cs`：容器操作、相機與 Canvas UI。
- `Assets/Editor/CreateLiquidProject.cs`：首次匯入的場景與 Universal 2D 設定。
- `Assets/Tests/Editor/LiquidPhysicsTests.cs`：靜置、晃動、傾斜與重設的粒子數／有限值／邊界檢查。

## 驗證

在 Unity 使用 **Window → General → Test Runner → EditMode → Run All**。測試涵蓋最低／最高黏度靜置、反覆晃動與傾斜後的粒子邊界、移動容器中的重設。測試不取代 Play Mode 畫面及手感驗收。

手動驗收：靜置後觀察液體沉到底部；拖動容器急停觀察慣性波浪；傾斜後液面應逐步朝世界水平恢復；比較黏度 0 與 1 的波浪衰減；確認滑桿不會同時拖動容器、暫停凍結、重設補回液體。測試不同 Game View 寬高比。

也可使用已授權的 Unity Editor 跑批次測試（將 Unity 路徑換成實際位置）：

```sh
/path/to/Unity -batchmode -nographics -projectPath "$PWD" \
  -runTests -testPlatform EditMode \
  -testResults /tmp/liquid-results.xml -logFile /tmp/liquid-tests.log
```

本次雲端沒有 Unity Editor／授權，**尚未執行 Unity 匯入、編譯、測試或畫面驗收**。URP 固定使用 Unity 6 的 `17.0.4`，測試套件為 `1.4.5`；這些依賴在指定的 6000.6.0f1 下仍需實際匯入確認。第一次匯入後 Unity 會產生 `.meta`、場景與設定資產，請將它們與 `Packages/packages-lock.json` 一起納入版本控制。不要忽略 `.meta` 檔案。
