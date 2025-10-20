using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GB;
using HarmonyLib;
using HeberekeBunnyGardenMod.Controllers;
using HeberekeBunnyGardenMod.Utils;
using UnityEngine;

namespace HeberekeBunnyGardenMod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static ConfigEntry<int> ConfigWidth;
    public static ConfigEntry<int> ConfigHeight;
    public static ConfigEntry<int> ConfigFrameRate;
    public static ConfigEntry<bool> ConfigUnlimited;
    public static ConfigEntry<bool> ConfigRemoveCensorLight;
    public static ConfigEntry<bool> ConfigNoDamage;
    public static ConfigEntry<bool> ConfigRegeneration;
    public static ConfigEntry<bool> ConfigNoFallDown;
    public static ConfigEntry<float> ConfigSensitivity;
    public static ConfigEntry<float> ConfigSpeed;
    public static ConfigEntry<float> ConfigFastSpeed;
    public static ConfigEntry<float> ConfigSlowSpeed;

    private GameObject freeCamObject;
    private Camera freeCam;
    private Camera originalCam;
    private GBInputFreeCameraController controller;
    public static bool isFreeCamActive = false;
    public static bool isFixedFreeCam = false;

    internal new static ManualLogSource Logger;

    private void Awake()
    {
        ConfigWidth = Config.Bind(
            "Resolution",              // セクション名
            "Width",                   // キー名
            1920,                      // デフォルト値
            "解像度の幅（横）を指定します"); // 説明

        ConfigHeight = Config.Bind(
            "Resolution",
            "Height",
            1080,
            "解像度の高さ（縦）を指定します");

        ConfigFrameRate = Config.Bind(
            "Resolution",
            "FrameRate",
            60,
            "フレームレート上限を指定します。-1にすると上限を撤廃します。");

        ConfigRemoveCensorLight = Config.Bind(
            "Censor",
            "RemoveCensorLight",
            false,
            "trueにするとヨガとスキンケアの際に不自然な光が差し込まなくなります。センシティブモードも貫通します。くれぐれも配信しないように。");

        ConfigNoDamage = Config.Bind(
            "Cheat",
            "NoDamage",
            false,
            "trueにするとダメージを受けなくなります。服も破けなくなります。でも橋から落ちるとアウト。");

        ConfigRegeneration = Config.Bind(
            "Cheat",
            "Regeneration",
            false,
            "trueにするとダメージは受けますがHPが0にはなりません。服を破きたいときはこっち。");

        ConfigNoFallDown = Config.Bind(
            "Cheat",
            "NoFallDown",
            false,
            "trueにするとヒートゲージマックス維持時に転倒しなくなります。");

        ConfigSensitivity = Config.Bind(
            "Camera",
            "Sensitivity",
            2f,
            "フリーカメラのマウス感度");
        ConfigSpeed = Config.Bind("Camera",
            "Speed",
            10f,
            "フリーカメラの移動速度");
        ConfigFastSpeed = Config.Bind("Camera",
            "FastSpeed",
            30f,
            "フリーカメラの高速移動速度（Shift）");
        ConfigSlowSpeed = Config.Bind("Camera",
            "SlowSpeed",
            2.5f,
            "フリーカメラの低速移動速度（Ctrl）");

        // Plugin startup logic
        Logger = base.Logger;
        PatchLogger.Initialize(Logger);
        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll();
        PatchLogger.LogInfo($"解像度パッチを適用しました: {Plugin.ConfigWidth.Value}x{Plugin.ConfigHeight.Value}");
    }

    private void OnGUI()
    {
        // F5キーでトグル
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F5)
        {
            ToggleFreeCam();
        }
        // F6で固定モード切替
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F6)
        {
            ToggleFixedFreeCam();
        }

        if (isFreeCamActive)
        {
            if (isFixedFreeCam)
            {
                GUI.color = Color.yellow;
                GUI.Label(new Rect(10, 40, 500, 30), "Fixed Free Camera Mode: ON (F6=TOGGLE)");
                GUI.color = Color.white;
            }
            GUI.color = Color.green;
            GUI.Label(new Rect(10, 10, 500, 30), "Free Camera: ON (F5=OFF, Arrow/WASD=Move, E/Q=UpDown)");
            GUI.color = Color.white;
        }
    }

    private void ToggleFreeCam()
    {
        isFreeCamActive = !isFreeCamActive;

        if (isFreeCamActive)
        {
            CreateFreeCam();
        }
        else
        {
            DestroyFreeCam();
            isFixedFreeCam = false;
        }

        Logger.LogInfo($"フリーカメラ: {(isFreeCamActive ? "ON" : "OFF")}");
    }

    private void ToggleFixedFreeCam()
    {
        if (isFreeCamActive)
        {
            isFixedFreeCam = !isFixedFreeCam;
            Logger.LogInfo($"フリーカメラ固定モード: {(isFixedFreeCam ? "ON" : "OFF")}");
        }
    }

    private void CreateFreeCam()
    {
        // 元のカメラを取得
        originalCam = Camera.main;
        if (originalCam == null)
        {
            Logger.LogWarning("メインカメラが見つかりません");
            return;
        }

        // フリーカメラオブジェクトを作成
        freeCamObject = new GameObject("FreeCam");
        freeCam = freeCamObject.AddComponent<Camera>();

        // カメラ設定をコピー
        freeCam.CopyFrom(originalCam);

        // 元のカメラの位置から開始
        freeCamObject.transform.position = originalCam.transform.position;
        freeCamObject.transform.rotation = originalCam.transform.rotation;

        // GBInput対応コントローラーを追加
        controller = freeCamObject.AddComponent<GBInputFreeCameraController>();

        // オーディオリスナーを追加
        freeCamObject.AddComponent<AudioListener>();

        // 元のカメラを無効化
        originalCam.enabled = false;
        var originalListener = originalCam.GetComponent<AudioListener>();
        if (originalListener != null)
        {
            originalListener.enabled = false;
        }

        Logger.LogInfo("フリーカメラを作成しました");
    }

    private void DestroyFreeCam()
    {
        if (freeCamObject != null)
        {
            Destroy(freeCamObject);
            freeCamObject = null;
            freeCam = null;
            controller = null;
        }

        // 元のカメラを再有効化
        if (originalCam != null)
        {
            originalCam.enabled = true;
            var originalListener = originalCam.GetComponent<AudioListener>();
            if (originalListener != null)
            {
                originalListener.enabled = true;
            }
        }
    }
}

[HarmonyPatch(typeof(GBSystem), "IsInputDisabled")]
public class CanvasScalerReflectionYoga1Patch
{
    private static void Postfix(ref bool __result)
    {
        // フリーカメラがONかつ、固定モードでない場合、入力無効化
        if (Plugin.isFreeCamActive && !Plugin.isFixedFreeCam)
        {
            __result = true;
        }
    }
}
