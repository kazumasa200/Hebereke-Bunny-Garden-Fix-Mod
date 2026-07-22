#if BIE6
using BepInEx.Unity.Mono;
#endif

using System;
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
    private static Plugin Instance;

    internal static event Action GUICallback;

    private Patches.FreeCamera.FreeCameraManager freeCamera;
    private bool isOverlayVisible = true;
    private bool isCapturingScreenshot;
    private static float suppressGameInputUntilUnscaledTime = -1f;
    private const float ControllerShortcutSuppressDuration = 0.18f;

    private static readonly string ScreenshotDirectory = Path.Combine(Paths.BepInExRootPath, "screenshots",
        MyPluginInfo.PLUGIN_GUID);

    internal new static ManualLogSource Logger;

    private void Awake()
    {
        Instance = this;
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

        PatchLogger.LogInfo($"プラグイン起動: {MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION}");
        PatchLogger.LogInfo($"解像度パッチを適用しました: {Configs.Width.Value}x{Configs.Height.Value}");
        PatchLogger.LogInfo($"アンチエイリアシング設定: {Configs.AntiAliasing.Value}");
    }

    private void OnDestroy()
    {
        if (ReferenceEquals(Instance, this))
            Instance = null;
    }

    private void Update()
    {
        if (Keyboard.current?[Key.F4].wasPressedThisFrame == true)
            Config.Reload();

        if (Configs.OverlayToggle.IsTriggered())
            ToggleOverlay();

        if (Configs.CaptureScreenshot.IsTriggered())
            CaptureScreenshot();
    }

    private void OnGUI()
    {
        if (!isOverlayVisible || isCapturingScreenshot)
            return;

        GUILayout.BeginArea(new Rect(10, 10, Screen.width / 2, Screen.height - 10));
        GUICallback?.Invoke();
        GUILayout.EndArea();
    }

    internal static void DisableFreeCamForSystemUiIfNeeded(string reason)
    {
        Instance?.freeCamera?.Deactivate();
        PatchLogger.LogInfo($"フリーカメラを自動解除しました: {reason}");
    }

    /// <summary>
    /// 一定時間 (0.18 秒) ゲーム本体側の入力およびホットキー判定を抑止する。
    /// コントローラーショートカット発火後の連続発火防止と、KeyBinding キャプチャ確定後の
    /// 同一キー再評価防止に使用。
    /// </summary>
    public static void SuppressGameInputTemporarily()
    {
        suppressGameInputUntilUnscaledTime = Time.unscaledTime + ControllerShortcutSuppressDuration;
    }

    /// <summary>SuppressGameInputTemporarily 期間中なら true。</summary>
    internal static bool ShouldSuppressGameInput()
    {
        return Time.unscaledTime < suppressGameInputUntilUnscaledTime;
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

    private void CaptureScreenshot()
    {
        StartCoroutine(CaptureScreenshotCoroutine());
    }

    private System.Collections.IEnumerator CaptureScreenshotCoroutine()
    {
        Camera captureCam = FindCurrentCamera();
        if (captureCam == null)
            yield break;

        isCapturingScreenshot = true;

        try
        {
            Directory.CreateDirectory(ScreenshotDirectory);
            string path = Path.Combine(ScreenshotDirectory, $"hbg_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
            ScreenCapture.CaptureScreenshot(path, Configs.ScreenshotScale.Value);
            PatchLogger.LogInfo($"スクリーンショットを保存しました: {path}");
        }
        catch (Exception ex)
        {
            PatchLogger.LogError($"スクリーンショット保存失敗: {ex.Message}");
        }

        // スクリーンショットがキャプチャされる前にオーバーレイを再表示しないよう、1フレーム待機
        yield return null;
        isCapturingScreenshot = false;
    }
}

/// <summary>フリーカメラ中（非固定）はゲーム本体の入力を無効化する。</summary>
[HarmonyPatch(typeof(GBSystem), "IsInputDisabled")]
public class FreeCamInputDisablePatch
{
    private static void Postfix(ref bool __result)
    {
        if (Patches.FreeCamera.FreeCameraManager.IsActive && !Patches.FreeCamera.FreeCameraManager.IsFixed)
            __result = true;
    }
}

/// <summary>終了確認ダイアログ表示時にフリーカメラを自動解除してカーソルを戻す。</summary>
[HarmonyPatch(typeof(GBSystem), "confirmQuit")]
public class FreeCamDisableOnQuitConfirmPatch
{
    private static void Prefix()
    {
        Plugin.DisableFreeCamForSystemUiIfNeeded("終了確認ダイアログ");
    }
}

/// <summary>
/// フリーカメラのコントローラーショートカット発火直後、および F9 設定パネルの
/// キーバインドキャプチャ中に、ゲーム本体側の入力を遮断する。
/// </summary>
[HarmonyPatch]
public class FreeCamControllerShortcutInputSuppressionPatch
{
    [HarmonyPatch(typeof(GBInput), "isTriggered")]
    [HarmonyPrefix]
    private static bool SuppressTriggered(InputAction button, ref bool __result)
        => TrySuppress(button, ref __result);

    [HarmonyPatch(typeof(GBInput), "isPressing")]
    [HarmonyPrefix]
    private static bool SuppressPressing(InputAction button, ref bool __result)
        => TrySuppress(button, ref __result);

    [HarmonyPatch(typeof(GBInput), "isReleased")]
    [HarmonyPrefix]
    private static bool SuppressReleased(InputAction button, ref bool __result)
        => TrySuppress(button, ref __result);

    [HarmonyPatch(typeof(GBInput), "isTriggeredR")]
    [HarmonyPrefix]
    private static bool SuppressTriggeredRepeat(ref bool __result)
    {
        // キャプチャ中はゲーム側の全リピート入力を遮断する
        if (Patches.Settings.SettingsController.IsAnyCapturing)
        {
            __result = false;
            return false;
        }
        if (!Patches.FreeCamera.FreeCameraManager.IsActive || !Plugin.ShouldSuppressGameInput())
            return true;

        __result = false;
        return false;
    }

    [HarmonyPatch(typeof(GBInput), "GetStickValue")]
    [HarmonyPrefix]
    private static bool SuppressStick(InputAction stick, ref Vector2 __result)
    {
        // キャプチャ中はゲーム側のスティック入力を遮断する
        if (Patches.Settings.SettingsController.IsAnyCapturing)
        {
            __result = Vector2.zero;
            return false;
        }
        if (!Patches.FreeCamera.FreeCameraManager.IsActive || !Plugin.ShouldSuppressGameInput())
            return true;

        if (stick?.activeControl?.device is not Gamepad)
            return true;

        __result = Vector2.zero;
        return false;
    }

    [HarmonyPatch(typeof(GBInput), "CameraControll")]
    [HarmonyPrefix]
    private static bool SuppressCameraControl(ref Vector2 __result)
    {
        // キャプチャ中はゲーム側のカメラ操作入力を遮断する
        if (Patches.Settings.SettingsController.IsAnyCapturing)
        {
            __result = Vector2.zero;
            return false;
        }
        if (!Patches.FreeCamera.FreeCameraManager.IsActive || !Plugin.ShouldSuppressGameInput())
            return true;

        __result = Vector2.zero;
        return false;
    }

    private static bool TrySuppress(InputAction button, ref bool result)
    {
        // キャプチャ中はゲーム側の全ボタン入力を遮断する
        if (Patches.Settings.SettingsController.IsAnyCapturing)
        {
            result = false;
            return false;
        }
        if (!Patches.FreeCamera.FreeCameraManager.IsActive || !Plugin.ShouldSuppressGameInput())
            return true;

        if (button?.activeControl?.device is not Gamepad)
            return true;

        result = false;
        return false;
    }
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
