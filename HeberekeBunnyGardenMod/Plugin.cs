using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GB;
using GB.MiniGame;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;

namespace HeberekeBunnyGardenMod
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<int> ConfigWidth;
        public static ConfigEntry<int> ConfigHeight;
        public static ConfigEntry<int> ConfigFrameRate;
        public static ConfigEntry<bool> ConfigUnlimited;
        public static ConfigEntry<bool> ConfigRemoveCensorLight;
        public static ConfigEntry<bool> ConfigNoDamage;
        public static ConfigEntry<bool> ConfigRegeneration;
        public static ConfigEntry<bool> ConfigNoFallDown;
        private HashSet<string> reportedObjects = new HashSet<string>();

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
                60,
                "フレームレート上限を指定します。-1にすると上限を撤廃します。");

            ConfigRemoveCensorLight = Config.Bind(
                "Censor",
                "RemoveCensorLight",
                false,
                "trueにするとヨガとスキンケアの際に不自然な光が差し込まなくなります。センシティブモードも貫通します。くれぐれも配信しないように。");

            ConfigNoDamage = Config.Bind(
                "Cheat",
                "NoDamage",
                false,
                "trueにするとダメージを受けなくなります。服も破けなくなります。でも橋から落ちるとアウト。");

            ConfigRegeneration = Config.Bind(
                "Cheat",
                "Regeneration",
                false,
                "trueにするとダメージは受けますがHPが0にはなりません。服を破きたいときはこっち。");

            ConfigNoFallDown = Config.Bind(
                "Cheat",
                "NoFallDown",
                false,
                "trueにするとヒートゲージマックス維持時に転倒しなくなります。");

            // Plugin startup logic
            Logger = base.Logger;
            var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
            Logger.LogInfo($"解像度パッチを適用しました: {ConfigWidth.Value}x{ConfigHeight.Value}");
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
                if (Plugin.ConfigFrameRate.Value < 0)
                {
                    // -1なら上限撤廃
                    Application.targetFrameRate = -1;
                    Debug.Log("フレームレートの上限を撤廃しました");
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

        [HarmonyPatch(typeof(GB.MiniGame.LoationGame), "Setup", new Type[] {
        typeof(GB.Character.Chara),
        typeof(CancellationToken)
    })]
        public class RemoveCensorLightLoation
        {
            // Postfixを使用（Setup完了後に実行）
            private static void Postfix(GB.MiniGame.LoationGame __instance)
            {
                try
                {
                    if (!Plugin.ConfigRemoveCensorLight.Value)
                    {
                        return;
                    }

                    if (__instance == null)
                    {
                        Debug.LogWarning("__instance is null");
                        return;
                    }

                    if (__instance.Chara == null)
                    {
                        Debug.LogWarning("__instance.Chara is null");
                        return;
                    }

                    if (__instance.Chara.CensorLights == null)
                    {
                        Debug.LogWarning("CensorLights is null");
                        return;
                    }

                    // CensorLightsを無効化
                    foreach (var light in __instance.Chara.CensorLights)
                    {
                        if (light != null)
                        {
                            light.SetActive(false);
                            Debug.Log("CensorLightを無効化しました");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"RemoveCensorLightLoation エラー: {ex.Message}");
                    Debug.LogError($"スタックトレース: {ex.StackTrace}");
                }
            }

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
    }
}