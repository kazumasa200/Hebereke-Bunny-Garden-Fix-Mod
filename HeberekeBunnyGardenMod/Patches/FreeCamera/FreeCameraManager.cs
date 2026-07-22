using System.Collections;
using System.Collections.Generic;
using GB;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;

namespace HeberekeBunnyGardenMod.Patches.FreeCamera;

/// <summary>
/// フリーカメラ（コア仕様）。メインディスプレイ出力のみ。
/// PiP / 別ディスプレイ出力は本 MOD では移植対象外。
/// </summary>
public class FreeCameraManager : MonoBehaviour
{
    public static bool IsActive { get; private set; } = false;
    public static bool IsFixed { get; private set; } = false;

    private Camera originalCam;
    private GameObject freeCamObject;
    private FreeCameraController controller;
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

        freeCamObject = new GameObject("HBGFreeCam");
        var freeCam = freeCamObject.AddComponent<Camera>();
        freeCam.CopyFrom(originalCam);

        freeCamObject.transform.SetPositionAndRotation(
            originalCam.transform.position,
            originalCam.transform.rotation);

        // メインディスプレイにフリーカメラを出力し、元のカメラは停止する。
        freeCam.targetDisplay = 0;
        originalCam.enabled = false;
        freeCamObject.AddComponent<AudioListener>();
        if (originalCam.TryGetComponent<AudioListener>(out var listener))
            listener.enabled = false;

        CopyUrpCameraData(originalCam, freeCam);
        controller = freeCamObject.AddComponent<FreeCameraController>();

        IsActive = true;
        IsFixed = false;

        Plugin.Logger.LogInfo("フリーカメラを作成しました");
        RefreshGameUiSuppression(force: true);
    }

    public void Deactivate()
    {
        if (freeCamObject != null)
        {
            var cam = freeCamObject.GetComponent<Camera>();
            if (cam != null)
                cam.targetTexture = null;
            StartCoroutine(BlackoutAndDestroyRoutine(freeCamObject)); // ブラックアウトしてから破棄
            freeCamObject = null;
            controller = null;
        }

        if (originalCam != null)
        {
            originalCam.enabled = true;
            originalCam.targetDisplay = 0;

            if (originalCam.TryGetComponent<AudioListener>(out var listener))
                listener.enabled = true;
        }

        IsActive = false;
        IsFixed = false;
        RefreshGameUiSuppression(force: true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Plugin.Logger.LogInfo("フリーカメラを解除しました");
    }

    private IEnumerator BlackoutAndDestroyRoutine(GameObject targetCamObject)
    {
        // 破棄前の1フレーム、画面全体を黒で塗りつぶして残像を防ぐ。
        var cam = targetCamObject.GetComponent<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.cullingMask = 0;

            yield return new WaitForEndOfFrame();
        }
        Destroy(targetCamObject);
    }

    private static void CopyUrpCameraData(Camera src, Camera dst)
    {
        var srcData = src.GetUniversalAdditionalCameraData();
        var dstData = dst.GetUniversalAdditionalCameraData();
        if (srcData == null || dstData == null)
            return;

        dstData.renderPostProcessing = srcData.renderPostProcessing;
        dstData.antialiasing = srcData.antialiasing;
        dstData.antialiasingQuality = srcData.antialiasingQuality;
        dstData.stopNaN = srcData.stopNaN;
        dstData.dithering = srcData.dithering;
        dstData.renderShadows = srcData.renderShadows;
        dstData.volumeLayerMask = srcData.volumeLayerMask;
        dstData.volumeTrigger = srcData.volumeTrigger;
    }

    public void RefreshGameUiSuppression(bool force = false)
    {
        // メインディスプレイをフリーカメラが占有しているため、
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

    private bool ShouldHideCanvas(Canvas canvas)
    {
        if (canvas == null)
            return false;

        if (freeCamObject != null && canvas.transform.IsChildOf(freeCamObject.transform))
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
