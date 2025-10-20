using HarmonyLib;

namespace HeberekeBunnyGardenMod.Patches
{
    [HarmonyPatch(typeof(GB.InGame.Player.PlayerActor), "DbgAutoHpRecovery", MethodType.Getter)]
    public class DbgAutoHpRecoveryPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (Plugin.ConfigRegeneration.Value == true)
            {
                __result = true;
            }
            else
            {
                __result = false;
            }
        }
    }
}
