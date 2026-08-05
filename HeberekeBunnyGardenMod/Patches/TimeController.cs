using UnityEngine;

namespace HeberekeBunnyGardenMod.Patches;

/// <summary>
/// 時間停止・スロー再生・早送りを <see cref="Time.timeScale"/> で制御する。
/// 停止／スローはトグル、早送りは押している間のみ有効。優先度は 早送り &gt; 停止 &gt; スロー。
/// </summary>
public class TimeController : MonoBehaviour
{
    private bool stop;
    private bool slow;
    private bool fastForward;
    private bool wasControlling;

    public static TimeController Initialize(GameObject parent)
        => parent.AddComponent<TimeController>();

    private void OnEnable()
    {
        Plugin.GUICallback += GUICallback;
    }

    private void OnDisable()
    {
        Plugin.GUICallback -= GUICallback;
        Time.timeScale = 1f;
        stop = false;
        slow = false;
    }

    private void Update()
    {
        fastForward = Configs.FastForward.IsHeld();

        if (Configs.TimeStopToggle.IsTriggered())
        {
            stop = !stop;
            if (stop) slow = false; // 停止に入るときスローを解除
            Plugin.Logger.LogInfo($"時間停止: {(stop ? "ON" : "OFF")}");
        }

        if (Configs.SlowMotionToggle.IsTriggered())
        {
            slow = !slow;
            if (slow) stop = false; // スローに入るとき停止を解除
            Plugin.Logger.LogInfo($"スロー再生: {(slow ? "ON" : "OFF")}");
        }
    }

    private void LateUpdate()
    {
        bool controlling = stop || slow || fastForward;

        if (fastForward)
            Time.timeScale = Configs.FastForwardSpeed.Value;
        else if (stop)
            Time.timeScale = 0f;
        else if (slow)
            Time.timeScale = Configs.SlowMotionScale.Value;
        else if (wasControlling)
            // MOD の制御から抜けた直後の 1 フレームだけ 1f に戻す。
            // 毎フレーム 1f を書くとゲーム側の時間演出（早送り等）を上書きしてしまうため。
            Time.timeScale = 1f;

        wasControlling = controlling;
    }

    private void GUICallback()
    {
        if (!stop && !slow)
            return;

        GUI.color = Color.cyan;
        if (stop)
            GUILayout.Label($"Time Stop: ON ({Configs.TimeStopToggle}=OFF)");
        if (slow)
            GUILayout.Label($"Slow Motion: {Configs.SlowMotionScale.Value:F2}x ({Configs.SlowMotionToggle}=OFF)");
        GUI.color = Color.white;
    }
}
