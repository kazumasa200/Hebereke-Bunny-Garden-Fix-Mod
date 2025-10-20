using GB;
using HarmonyLib;
using HeberekeBunnyGardenMod.Utils;
using UnityEngine;

namespace HeberekeBunnyGardenMod.Patches;

/// <summary>
/// フレームレートを設定するパッチ
/// </summary>
[HarmonyPatch(typeof(GBSystem), "Setup")]
public class SetRefreshRatePatch
{
    private static void Postfix()
    {
        if (Plugin.ConfigFrameRate.Value < 0)
        {
            // -1なら上限撤廃
            Application.targetFrameRate = -1;
            PatchLogger.LogInfo("フレームレートの上限を撤廃しました");
            return;
        }
        // 指定したフレームレートに設定
        Application.targetFrameRate = Plugin.ConfigFrameRate.Value;
        PatchLogger.LogInfo($"フレームレートを {Plugin.ConfigFrameRate.Value} FPS に設定しました");
    }
}
