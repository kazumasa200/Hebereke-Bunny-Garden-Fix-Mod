using HarmonyLib;

namespace HeberekeBunnyGardenMod.Patches;

/// <summary>
/// 無敵モードのパッチ
/// </summary>
[HarmonyPatch(typeof(GB.InGame.Player.PlayerActor), "DbgInvincible", MethodType.Getter)]
public class DbgInvinciblePatch
{
    private static void Postfix(ref bool __result)
    {
        if (Plugin.ConfigNoDamage.Value == true)
        {
            __result = true;
        }
        else
        {
            __result = false;
        }
    }
}
