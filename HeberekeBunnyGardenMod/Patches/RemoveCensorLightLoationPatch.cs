using GB.MiniGame;
using HarmonyLib;
using HeberekeBunnyGardenMod.Utils;
using System;
using System.Threading;

namespace HeberekeBunnyGardenMod.Patches;

/// <summary>
/// スキンケアで不自然な光を消すパッチ
/// </summary>
public class RemoveCensorLightLoationPatch
{
    [HarmonyPatch(typeof(LoationGame), "Setup", [typeof(bool), typeof(GB.Character.Chara), typeof(CancellationToken)])]
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

                if (__instance.Chara.CensorLight == null)
                {
                    PatchLogger.LogWarning("CensorLights is null");
                    return;
                }

                __instance.Chara.CensorLight.SetActive(false);
                PatchLogger.LogInfo("CensorLightを無効化しました");
            }
            catch (Exception ex)
            {
                PatchLogger.LogError($"RemoveCensorLightLoation エラー: {ex.Message}");
                PatchLogger.LogError($"スタックトレース: {ex.StackTrace}");
            }
        }
    }

    [HarmonyPatch(typeof(LoationGame.Charas), "AttachCensorLight", [typeof(bool)])]
    public class DisableAttachCensorLight
    {
        private static bool Prefix(LoationGame.Charas __instance)
        {
            if (!Plugin.ConfigRemoveCensorLight.Value)
            {
                return true;
            }
            if (__instance.CensorLight != null)
            {
                __instance.CensorLight.SetActive(false);
            }
            return false;
        }
    }
}
