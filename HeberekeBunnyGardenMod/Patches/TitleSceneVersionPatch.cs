using HeberekeBunnyGardenMod.Utils;
using GB.Scene;
using HarmonyLib;
using System;
using TMPro;
using UnityEngine;

namespace HeberekeBunnyGardenMod.Patches;

/// <summary>
/// タイトル画面のゲームバージョン表示（<c>VersionText</c>）の 1 行上に
/// MOD バージョンラベルを追加するパッチ。
///
/// <para>
/// へべれけ版の <c>TitleScene.Setup()</c> は <c>async UniTask</c> だが、
/// <c>VersionText.text</c> の設定は最初の <c>await</c> より前に行われるため、
/// Postfix 発火時点でバージョン表示は確定している。
/// </para>
///
/// <para>
/// 新しいラベルは <c>VersionText</c> の RectTransform・フォント・サイズ・カラー・
/// アライメントをそのままコピーし、anchoredPosition を 1 行分（fontSize × 1.3）上にずらす。
/// ゲームアップデートでフォントや色が変わっても自動的に追従する。
/// </para>
/// </summary>
[HarmonyPatch(typeof(TitleScene), "Setup")]
public static class TitleSceneVersionPatch
{
    private static void Postfix(TitleScene __instance)
    {
        try
        {
            // バージョン表示フィールド VersionText を取得（public だが reflection で堅牢に）
            var field = AccessTools.Field(typeof(TitleScene), "VersionText");
            if (field == null)
            {
                PatchLogger.LogWarning("[TitleVersion] フィールド VersionText が見つかりませんでした");
                return;
            }

            var verText = field.GetValue(__instance) as TextMeshProUGUI;
            if (verText == null)
            {
                PatchLogger.LogWarning("[TitleVersion] VersionText が null または TextMeshProUGUI 型ではありませんでした");
                return;
            }

            // VersionText と同じ親に MOD バージョンラベルを追加する
            var verRt = verText.rectTransform;
            var parent = verRt.parent;

            var go = new GameObject("HBGModVersionText");
            go.transform.SetParent(parent, false);

            // RectTransform を VersionText に揃える
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = verRt.anchorMin;
            rt.anchorMax = verRt.anchorMax;
            rt.pivot = verRt.pivot;
            rt.sizeDelta = verRt.sizeDelta;

            // 1 行分上にずらす（fontSize × 1.3 は TMP デフォルト行ピッチの目安）
            float lineStep = verText.fontSize * 1.3f;
            rt.anchoredPosition = verRt.anchoredPosition + new Vector2(0f, lineStep);

            // TextMeshProUGUI をゲームバージョンと同スタイルで追加
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = verText.font;
            tmp.fontSize = verText.fontSize;
            tmp.color = verText.color;
            tmp.alignment = verText.alignment;
            tmp.text = $"Mod v{MyPluginInfo.PLUGIN_VERSION}";

            PatchLogger.LogInfo($"[TitleVersion] MOD バージョンラベルを追加しました: Mod v{MyPluginInfo.PLUGIN_VERSION}");
        }
        catch (Exception ex)
        {
            PatchLogger.LogWarning($"[TitleVersion] MOD バージョンラベルの追加に失敗しました: {ex.Message}");
        }
    }
}
