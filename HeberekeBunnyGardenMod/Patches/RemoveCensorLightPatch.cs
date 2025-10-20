using GB.MiniGame;
using HarmonyLib;

namespace HeberekeBunnyGardenMod.Patches;

/// <summary>
/// ヨガで不自然な光を消すパッチ
/// </summary>
[HarmonyPatch(typeof(YogaGame.Charas), "SetTopless")]
public class RemoveCensorLightPatch
{
    private static bool Prefix(YogaGame.Charas __instance, bool v)
    {
        // 元のコードを再現しつつ、CensorLightだけfalseに変更
        __instance.Topless.SetActive(true);
        __instance.Normal.SetActive(!v);
        if (Plugin.ConfigRemoveCensorLight.Value == true)
        {
            __instance.CensorLight.SetActive(false);
        }
        else
        {
            __instance.CensorLight.SetActive(v);
        }
        __instance.Anim2.Animator.transform.GetChild(1).gameObject.SetActive(v);

        // 元のメソッドをスキップ
        return false;
    }
}
