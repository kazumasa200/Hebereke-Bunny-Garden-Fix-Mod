using System.Collections.Generic;
using GB;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HeberekeBunnyGardenMod.Patches.FreeCamera;

/// <summary>
/// フリーカメラ（コア仕様・カメラ乗っ取り方式）。
///
/// <para>
/// 新規カメラを複製すると、ゲーム側がアクティブカメラに対して行う描画変更
/// （Cinemachine 制御・cullingMask 変更など。特にミニゲームやトルン演出）が
/// 反映されず、キャラが映らなくなる。これを避けるため、ゲーム本編が使用中の
/// カメラをそのまま乗っ取り（CinemachineBrain を一時無効化して transform を直接操作）、
/// 解除時に元へ戻す。これによりゲーム本来の描画設定を完全に維持する。
/// </para>
/// PiP / 別ディスプレイ出力は本 MOD では対象外。
/// </summary>
public class FreeCameraManager : MonoBehaviour
{
    public static bool IsActive { get; private set; } = false;
    public static bool IsFixed { get; private set; } = false;

    private Camera originalCam;
    private FreeCameraController controller;
    private Behaviour cinemachineBrain;      // 乗っ取り中は無効化する（解除時に復元）
    private Vector3 savedPosition;
    private Quaternion savedRotation;
    private readonly Dictionary<EventSystem, bool> eventSystemNavigationStates = [];
    private readonly Dictionary<Canvas, bool> canvasEnabledStates = [];
    private bool isGameUiSuppressed;

    public static FreeCameraManager Initialize(GameObject parent)
        => parent.AddComponent<FreeCameraManager>();

    private void OnEnable()
    {
        Plugin.GUICallback += GUICallback;
    }

    private void OnDisable()
    {
        Plugin.GUICallback -= GUICallback;
        Deactivate();
    }

    private void Update()
    {
        // 乗っ取り中にカメラがシーン遷移等で失われたら自動解除する。
        if (IsActive && originalCam == null)
            Deactivate();

        if (Configs.FreeCamToggle.IsTriggered())
            ToggleFreeCam();

        if (Configs.FixedFreeCamToggle.IsTriggered())
            ToggleFixedFreeCam();
    }

    private void ToggleFreeCam()
    {
        if (IsActive)
            Deactivate();
        else
            Activate();
    }

    private void ToggleFixedFreeCam()
    {
        if (!IsActive)
            return;
        IsFixed = !IsFixed;
        if (controller != null)
            controller.enabled = !IsFixed;
        RefreshGameUiSuppression(force: true);
        Plugin.Logger.LogInfo($"フリーカメラ固定モード: {(IsFixed ? "ON" : "OFF")}");
    }

    private void Activate()
    {
        originalCam = Plugin.FindCurrentCamera();
        if (originalCam == null)
        {
            IsActive = false;
            return;
        }

        // 現在の位置・回転を保存（解除時に戻す）
        savedPosition = originalCam.transform.position;
        savedRotation = originalCam.transform.rotation;

        // Cinemachine が毎フレーム transform を上書きするため、Brain を一時無効化する。
        // Cinemachine アセンブリを参照せずに済むよう型名で取得する。
        cinemachineBrain = originalCam.GetComponent("CinemachineBrain") as Behaviour;
        if (cinemachineBrain != null)
            cinemachineBrain.enabled = false;

        // ゲーム本編のカメラ自身に操作コンポーネントを付けて transform を直接動かす。
        controller = originalCam.gameObject.AddComponent<FreeCameraController>();
        controller.enabled = !IsFixed;

        IsActive = true;
        IsFixed = false;

        Plugin.Logger.LogInfo($"フリーカメラを有効化しました（乗っ取り: {originalCam.name}）");
        RefreshGameUiSuppression(force: true);
    }

    public void Deactivate()
    {
        if (controller != null)
        {
            Destroy(controller);
            controller = null;
        }

        // Cinemachine Brain を復帰し、カメラを元の位置へ戻す。
        if (cinemachineBrain != null)
        {
            cinemachineBrain.enabled = true;
            cinemachineBrain = null;
        }

        if (originalCam != null)
            originalCam.transform.SetPositionAndRotation(savedPosition, savedRotation);

        originalCam = null;

        IsActive = false;
        IsFixed = false;
        RefreshGameUiSuppression(force: true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Plugin.Logger.LogInfo("フリーカメラを解除しました");
    }

    public void RefreshGameUiSuppression(bool force = false)
    {
        // フリーカメラ中・非固定・かつシステムUI非表示のときにゲームUI入力を抑止する。
        bool shouldSuppress = IsActive && !IsFixed && !ShouldExposeGameUiDuringFreeCam();
        if (!force && shouldSuppress == isGameUiSuppressed)
            return;

        isGameUiSuppressed = shouldSuppress;

        EventSystem[] eventSystems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (!shouldSuppress)
        {
            foreach (var pair in eventSystemNavigationStates)
            {
                if (pair.Key != null)
                    pair.Key.sendNavigationEvents = pair.Value;
            }
            eventSystemNavigationStates.Clear();

            foreach (var pair in canvasEnabledStates)
            {
                if (pair.Key != null)
                    pair.Key.enabled = pair.Value;
            }
            canvasEnabledStates.Clear();
            return;
        }

        foreach (var eventSystem in eventSystems)
        {
            if (eventSystem == null)
                continue;

            if (!eventSystemNavigationStates.ContainsKey(eventSystem))
                eventSystemNavigationStates[eventSystem] = eventSystem.sendNavigationEvents;

            eventSystem.sendNavigationEvents = false;
            eventSystem.SetSelectedGameObject(null);
        }

        if (!Configs.HideGameUiInFreeCam.Value)
            return;

        foreach (var canvas in canvases)
        {
            if (!ShouldHideCanvas(canvas))
                continue;

            if (!canvasEnabledStates.ContainsKey(canvas))
                canvasEnabledStates[canvas] = canvas.enabled;

            canvas.enabled = false;
        }
    }

    private static bool ShouldHideCanvas(Canvas canvas)
    {
        if (canvas == null)
            return false;

        return canvas.renderMode != RenderMode.WorldSpace;
    }

    private static bool ShouldExposeGameUiDuringFreeCam()
    {
        var gbSystem = GBSystem.Instance;
        if (gbSystem == null)
            return false;

        // へべれけ版: IsPauseMenuActive はプロパティ。
        if (gbSystem.IsInConfirmQuit || gbSystem.IsPauseMenuActive)
            return true;

        var confirmDialog = gbSystem.GetConfirmDialog();
        return confirmDialog != null && confirmDialog.IsActive();
    }

    private void GUICallback()
    {
        if (!IsActive)
            return;

        GUI.color = Color.white;
        GUILayout.Label(
            "Move: Arrow/WASD or Left Stick, Up/Down: E/Q or ZR/ZL, Look: Mouse or Right Stick, Speed: Shift/Ctrl or R/L");
        GUI.color = Color.green;
        GUILayout.Label($"Free Camera: ON ({Configs.FreeCamToggle}=OFF)");
        GUI.color = Color.yellow;
        GUILayout.Label($"Fixed Mode: {(IsFixed ? "ON" : "OFF")} ({Configs.FixedFreeCamToggle}=TOGGLE)");
        GUI.color = Color.white;
    }
}
