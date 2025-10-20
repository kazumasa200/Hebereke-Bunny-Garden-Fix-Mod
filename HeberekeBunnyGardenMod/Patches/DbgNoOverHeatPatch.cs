using HarmonyLib;

namespace HeberekeBunnyGardenMod.Patches;

[HarmonyPatch(typeof(GB.InGame.Player.PlayerActor), "DbgNoOverheat", MethodType.Getter)]
public class DbgNoOverHeatPatch
{
    private static void Postfix(ref bool __result)
    {
        if (Plugin.ConfigNoFallDown.Value == true)
        {
            __result = true;
        }
        else
        {
            __result = false;
        }
    }
}
