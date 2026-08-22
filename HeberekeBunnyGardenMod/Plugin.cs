#if BIE6
using BepInEx.Unity.Mono;
#endif

using System;
using System.Collections;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using GB;
using HarmonyLib;
using HeberekeBunnyGardenMod.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HeberekeBunnyGardenMod;

public enum AntiAliasingType
{
    Off,
    FXAA,
    TAA,
    MSAA2x,
    MSAA4x,
    MSAA8x,
}

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private static Plugin s_instance;

    internal static event Action GUICallback;

    private Patches.FreeCamera.FreeCameraManager freeCamera;
    private bool isOverlayVisible = true;
    private bool hideOverlayForShot;

    internal new static ManualLogSource Logger;

    private void Awake()
    {
        s_instance = this;
        Logger = base.Logger;
        PatchLogger.Initialize(Logger);
        ConfigMigration.Migrate(Config);

        // YAML 駆動 Config（source of truth: Configs.yaml → Generated/Configs.g.cs）。
        // HotkeyConfig（KB+Pad 統合型）も BindAll 内で初期化される。
        Configs.BindAll(Config);

        // F9 パネルのグループ折りたたみ状態を .cfg から復元する（UI 非表示の内部状態）。
        Patches.Settings.SettingsCollapseState.Init(Config);

        if (Configs.UpdateCheck.Value)
            StartCoroutine(UpdateChecker.Check());

        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll();

        Patches.Settings.SettingsController.Initialize(gameObject);
        freeCamera = Patches.FreeCamera.FreeCameraManager.Initialize(gameObject);
        Patches.TimeController.Initialize(gameObject);

        PatchLogger.LogInfo($"プラグイン起動: {MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION}");
        PatchLogger.LogInfo($"解像度パッチを適用しました: {Configs.Width.Value}x{Configs.Height.Value}");
        PatchLogger.LogInfo($"アンチエイリアシング設定: {Configs.AntiAliasing.Value}");
    }

    private void OnDestroy()
    {
        if (s_instance == this)
            s_instance = null;
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb[Key.F4].wasPressedThisFrame)
            Config.Reload();

        if (Configs.OverlayToggle.IsTriggered())
            ToggleOverlay();

        if (Configs.CaptureScreenshot.IsTriggered())
            StartCoroutine(CaptureScreenshotRoutine());
    }

    private void OnGUI()
    {
        if (!isOverlayVisible || hideOverlayForShot)
            return;

        GUILayout.BeginArea(new Rect(10, 10, Screen.width / 2, Screen.height - 10));
        GUICallback?.Invoke();
        GUILayout.EndArea();
    }

    internal static void ReleaseFreeCameraFor(string uiName)
    {
        s_instance?.freeCamera?.Deactivate();
        PatchLogger.LogInfo($"システム UI が開いたためフリーカメラを終了します: {uiName}");
    }

    internal static Camera FindCurrentCamera()
    {
        var mainCam = Camera.main;
        if (mainCam != null)
        {
            Logger.LogInfo($"Camera.main = {mainCam.name}");
            return mainCam;
        }

        // tag に頼らず depth 最大の有効なカメラを代替として使用
        var cam = Camera.allCameras.OrderByDescending(c => c.depth).FirstOrDefault();
        if (cam == null)
        {
            Logger.LogError("有効なカメラが見つかりません。");
            return null;
        }
        Logger.LogInfo($"代替カメラを使用: {cam.name}");
        return cam;
    }

    private void ToggleOverlay()
    {
        isOverlayVisible = !isOverlayVisible;
        PatchLogger.LogInfo($"表示: {(isOverlayVisible ? "ON" : "OFF")}");
    }

    private IEnumerator CaptureScreenshotRoutine()
    {
        if (FindCurrentCamera() != null)
        {
            // オーバーレイを写り込ませないため、保存が終わる翌フレームまで隠す
            hideOverlayForShot = true;

            var dir = Path.Combine(Paths.BepInExRootPath, "screenshots", MyPluginInfo.PLUGIN_GUID);
            var file = Path.Combine(dir, $"hbg_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
            try
            {
                Directory.CreateDirectory(dir);
                ScreenCapture.CaptureScreenshot(file, Configs.ScreenshotScale.Value);
                PatchLogger.LogInfo($"スクリーンショットを保存しました: {file}");
            }
            catch (Exception e)
            {
                PatchLogger.LogError($"スクリーンショット保存失敗: {e.Message}");
            }

            yield return null;
            hideOverlayForShot = false;
        }
    }
}

/// <summary>フリーカメラ中（非固定）はゲーム本体の入力を無効化する。</summary>
[HarmonyPatch(typeof(GBSystem), "IsInputDisabled")]
public static class FreeCamGameInputDisablePatch
{
    private static void Postfix(ref bool __result)
    {
        if (Patches.FreeCamera.FreeCameraManager.IsActive && !Patches.FreeCamera.FreeCameraManager.IsFixed)
            __result = true;
    }
}

/// <summary>終了確認ダイアログが出たらフリーカメラを解除してカーソルを操作可能に戻す。</summary>
[HarmonyPatch(typeof(GBSystem), "confirmQuit")]
public static class QuitConfirmFreeCamReleasePatch
{
    private static void Prefix()
        => Plugin.ReleaseFreeCameraFor("終了確認");
}

/// <summary>
/// F9 設定パネル上（ポインタがパネル矩形内）またはキーバインドキャプチャ中は、
/// マウスクリックがゲーム側に貫通しないよう GBInput.isMouseTriggered を false に差し替える。
/// </summary>
[HarmonyPatch(typeof(GBInput), "isMouseTriggered")]
public class SuppressClickOverPanelPatch
{
    private static bool Prefix(ref bool __result)
    {
        if (Patches.Settings.SettingsController.IsAnyCapturing ||
            Patches.Settings.SettingsController.ShouldSuppressMouseInput())
        {
            __result = false;
            return false;
        }
        return true;
    }
}

/// <summary>
/// F9 設定パネル上またはキャプチャ中は、マウスホイールがゲーム側操作に流れないよう
/// GBInput.ScrollAxis を 0 に差し替える（UI Toolkit 内のスクロールは影響を受けない）。
/// </summary>
[HarmonyPatch]
public class SuppressScrollOverPanelPatch
{
    private static System.Reflection.MethodBase TargetMethod()
        => AccessTools.PropertyGetter(typeof(GBInput), nameof(GBInput.ScrollAxis));

    private static bool Prefix(ref float __result)
    {
        if (Patches.Settings.SettingsController.IsAnyCapturing ||
            Patches.Settings.SettingsController.ShouldSuppressMouseInput())
        {
            __result = 0f;
            return false;
        }
        return true;
    }
}
