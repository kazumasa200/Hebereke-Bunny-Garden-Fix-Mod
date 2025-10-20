using GB.MiniGame;
using HarmonyLib;
using HeberekeBunnyGardenMod.Utils;
using System;
using System.Threading;

namespace HeberekeBunnyGardenMod.Patches;

/// <summary>
/// スキンケアで不自然な光を消すパッチ
/// </summary>
[HarmonyPatch(typeof(LoationGame), "Setup", [typeof(GB.Character.Chara), typeof(CancellationToken)])]
public class RemoveCensorLightLoation
{
    private static void Postfix(LoationGame __instance)
    {
        try
        {
            if (!Plugin.ConfigRemoveCensorLight.Value)
            {
                return;
            }

            if (__instance == null)
            {
                PatchLogger.LogWarning("__instance is null");
                return;
            }

            if (__instance.Chara == null)
            {
                PatchLogger.LogWarning("__instance.Chara is null");
                return;
            }

            if (__instance.Chara.CensorLights == null)
            {
                PatchLogger.LogWarning("CensorLights is null");
                return;
            }

            // CensorLightsを無効化
            foreach (var light in __instance.Chara.CensorLights)
            {
                if (light != null)
                {
                    light.SetActive(false);
                    PatchLogger.LogInfo("CensorLightを無効化しました");
                }
            }
        }
        catch (Exception ex)
        {
            PatchLogger.LogError($"RemoveCensorLightLoation エラー: {ex.Message}");
            PatchLogger.LogError($"スタックトレース: {ex.StackTrace}");
        }
    }
}
