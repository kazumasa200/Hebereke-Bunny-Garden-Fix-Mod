using GB.InGame.Player;
using HarmonyLib;
using HeberekeBunnyGardenMod.Utils;
using System;
using UnityEngine;

namespace HeberekeBunnyGardenMod.Patches;

/// <summary>
/// 服が破けたときのオーバーレイ表示のCanvasScalerを修正するパッチ
/// </summary>
public class FixTornOverlayPatch
{
    [HarmonyPatch(typeof(TornTimeline), "PlayAsync")]
    public class CanvasScalerReflectionPatch
    {
        private static void Postfix(TornTimeline __instance)
        {
            Transform canvas = __instance.transform.Find("Canvas");
            if (canvas == null) return;

            FixCanvasScalerWithReflection(canvas.gameObject);
        }

        private static void FixCanvasScalerWithReflection(GameObject canvasObj)
        {
            // CanvasScalerコンポーネントを取得
            var scalerType = Type.GetType("UnityEngine.UI.CanvasScaler, UnityEngine.UI");
            if (scalerType == null)
            {
                PatchLogger.LogWarning("CanvasScaler型が見つかりません");
                return;
            }

            Component scaler = canvasObj.GetComponent(scalerType);
            if (scaler == null)
            {
                scaler = canvasObj.AddComponent(scalerType);
            }

            // uiScaleModeを設定（ScaleWithScreenSize = 1）
            var uiScaleModeField = scalerType.GetField("m_UiScaleMode",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (uiScaleModeField != null)
            {
                uiScaleModeField.SetValue(scaler, 1); // ScaleWithScreenSize
            }

            // referenceResolutionを設定
            var referenceResolutionField = scalerType.GetField("m_ReferenceResolution",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (referenceResolutionField != null)
            {
                referenceResolutionField.SetValue(scaler, new Vector2(1920, 1080));
            }

            // screenMatchModeを設定（MatchWidthOrHeight = 0）
            var screenMatchModeField = scalerType.GetField("m_ScreenMatchMode",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (screenMatchModeField != null)
            {
                screenMatchModeField.SetValue(scaler, 0); // MatchWidthOrHeight
            }

            // matchWidthOrHeightを設定（0.5 = 両方）
            var matchField = scalerType.GetField("m_MatchWidthOrHeight",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (matchField != null)
            {
                matchField.SetValue(scaler, 0.5f);
            }

            PatchLogger.LogInfo("Canvas Scalerを Scale With Screen Size に変更しました");
        }
    }

    [HarmonyPatch(typeof(YogaGameTimeline), "PlayAsync")]
    public class CanvasScalerReflectionYogaPatch
    {
        private static void Postfix(YogaGameTimeline __instance)
        {
            Transform canvas = __instance.transform.Find("Canvas");
            if (canvas == null) return;

            FixCanvasScalerWithReflection(canvas.gameObject);
        }

        private static void FixCanvasScalerWithReflection(GameObject canvasObj)
        {
            // CanvasScalerコンポーネントを取得
            var scalerType = Type.GetType("UnityEngine.UI.CanvasScaler, UnityEngine.UI");
            if (scalerType == null)
            {
                PatchLogger.LogWarning("CanvasScaler型が見つかりません");
                return;
            }

            Component scaler = canvasObj.GetComponent(scalerType);
            if (scaler == null)
            {
                scaler = canvasObj.AddComponent(scalerType);
            }

            // uiScaleModeを設定（ScaleWithScreenSize = 1）
            var uiScaleModeField = scalerType.GetField("m_UiScaleMode",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (uiScaleModeField != null)
            {
                uiScaleModeField.SetValue(scaler, 1); // ScaleWithScreenSize
            }

            // referenceResolutionを設定
            var referenceResolutionField = scalerType.GetField("m_ReferenceResolution",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (referenceResolutionField != null)
            {
                referenceResolutionField.SetValue(scaler, new Vector2(1920, 1080));
            }

            // screenMatchModeを設定（MatchWidthOrHeight = 0）
            var screenMatchModeField = scalerType.GetField("m_ScreenMatchMode",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (screenMatchModeField != null)
            {
                screenMatchModeField.SetValue(scaler, 0); // MatchWidthOrHeight
            }

            // matchWidthOrHeightを設定（0.5 = 両方）
            var matchField = scalerType.GetField("m_MatchWidthOrHeight",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (matchField != null)
            {
                matchField.SetValue(scaler, 0.5f);
            }

            PatchLogger.LogInfo("Canvas Scalerを Scale With Screen Size に変更しました");
        }
    }
}
