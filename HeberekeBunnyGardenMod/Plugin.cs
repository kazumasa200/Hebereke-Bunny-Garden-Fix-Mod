using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GB;
using GB.Character;
using GB.MiniGame;
using HarmonyLib;
using System;
using UnityEngine;

namespace HeberekeBunnyGardenMod
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<int> ConfigWidth;
        public static ConfigEntry<int> ConfigHeight;
        public static ConfigEntry<sbyte> ConfigFrameRate;
        public static ConfigEntry<bool> ConfigUnlimited;
        public static ConfigEntry<bool> ConfigRemoveCensorLight;

        internal new static ManualLogSource Logger;

        private void Awake()
        {
            ConfigWidth = Config.Bind(
                "Resolution",              // セクション名
                "Width",                   // キー名
                1920,                      // デフォルト値
                "解像度の幅（横）を指定します"); // 説明

            ConfigHeight = Config.Bind(
                "Resolution",
                "Height",
                1080,
                "解像度の高さ（縦）を指定します");

            ConfigFrameRate = Config.Bind(
                "Resolution",
                "FrameRate",
                (sbyte)60,
                "フレームレートを指定します(128まで)");

            ConfigRemoveCensorLight = Config.Bind(
                "Censor",
                "RemoveCensorLight",
                false,
                "trueにするとヨガの際に不自然な光が差し込まなくなります。センシティブモードも貫通します。くれぐれも配信しないように。");

            // Plugin startup logic
            Logger = base.Logger;
            var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
            Logger.LogInfo($"解像度パッチを適用しました: {ConfigWidth.Value}x{ConfigHeight.Value}");
        }
    }

    [HarmonyPatch(typeof(GBSystem), "CalcFullScreenResolution")]
    public class CalcFullScreenResolutionPatch
    {
        private static bool Prefix(ref ValueTuple<int, int, bool> __result)
        {
            // コンフィグから値を取得
            int num = Plugin.ConfigWidth.Value;
            int num2 = Plugin.ConfigHeight.Value;
            bool flag = true;
            float num3 = (float)num / (float)num2;
            Resolution currentResolution = Screen.currentResolution;
            float num4 = (float)currentResolution.width / (float)currentResolution.height;

            if (num4 > num3)
            {
                num2 = Mathf.Min(num2, currentResolution.height);
                num = (int)((float)num2 * num3);
                flag = false;
            }
            else if (num4 < num3)
            {
                num = Mathf.Min(num, currentResolution.width);
                num2 = (int)((float)num / num3);
            }

            __result = new ValueTuple<int, int, bool>(num, num2, flag);
            return false;
        }
    }

    [HarmonyPatch(typeof(GBSystem), "Setup")]
    public class SetRefreshRatePatch
    {
        private static void Postfix()
        {
            if (Plugin.ConfigFrameRate.Value < 1 || Plugin.ConfigFrameRate.Value > 128)
            {
                Debug.LogWarning("フレームレートの値が不正です。1から128の間で指定してください。");
                return;
            }

            // 指定したフレームレートに設定
            Application.targetFrameRate = Plugin.ConfigFrameRate.Value;
            Debug.Log($"フレームレートを {Plugin.ConfigFrameRate.Value} FPS に設定しました");
        }
    }

    [HarmonyPatch(typeof(GB.MiniGame.YogaGame.Charas), "SetTopless")]
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

    [HarmonyPatch(typeof(GB.MiniGame.LoationGame.Charas), "Setup")]
    public class RemoveCensorLightLoation
    {
        private static void PostFix(LoationGame.Charas __instance)
        {
            if (Plugin.ConfigRemoveCensorLight.Value)
            {
                foreach (var light in __instance.CensorLights)
                {
                    light.SetActive(false);
                }
                var pants = __instance.Model.GetComponentInChildren<OutfitSwitcher>().Pants;
                foreach (var pant in pants.Meshes)
                {
                    pant.Mesh.enabled = false;
                }
            }
        }
    }
}