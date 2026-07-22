using System.Collections.Generic;
using BepInEx.Configuration;

namespace HeberekeBunnyGardenMod.Patches.Settings;

/// <summary>
/// F9 設定パネルのグループ折りたたみ状態を .cfg に永続化する。
/// ConfigGen は string 型を非対応のため、YAML ではなく手書きで ConfigEntry&lt;string&gt; を Bind する。
/// デフォルトは全展開。折りたたんだグループのキーだけを集合として保存する。
/// 破損/未知トークン（旧フォーマット・手編集等）は HashSet に取り込んでも実在グループと照合されないだけで無害に無視される。
/// </summary>
internal static class SettingsCollapseState
{
    // section と group を区切る制御文字 Unit Separator (U+001F)。
    private const char KeySep = (char)0x1f;
    // 集合要素 (key) 同士を区切る制御文字 Record Separator (U+001E)。
    // ',' だと group/section 名に ',' が含まれると保存/復元でトークンが分裂するため、制御文字を使う。
    private const char TokenSep = (char)0x1e;

    private static ConfigEntry<string> s_entry;
    private static readonly HashSet<string> s_collapsed = new();

    /// <summary>Plugin.Awake から 1 回呼ぶ。.cfg の保存値を s_collapsed に復元する。</summary>
    public static void Init(ConfigFile cfg)
    {
        s_entry = cfg.Bind(
            "Internal", "SettingsCollapsedGroups", string.Empty,
            new ConfigDescription("【内部状態】F9 パネルで折りたたみ中のグループ。手動編集しないでください。"));

        s_collapsed.Clear();
        foreach (var token in s_entry.Value.Split(TokenSep))
        {
            if (!string.IsNullOrEmpty(token)) s_collapsed.Add(token);
        }
    }

    public static bool IsCollapsed(string section, string group)
    {
        if (string.IsNullOrEmpty(group)) return false;
        return s_collapsed.Contains(MakeKey(section, group));
    }

    public static void SetCollapsed(string section, string group, bool collapsed)
    {
        if (string.IsNullOrEmpty(group)) return;
        var key = MakeKey(section, group);
        bool changed = collapsed ? s_collapsed.Add(key) : s_collapsed.Remove(key);
        if (!changed) return;
        if (s_entry != null) s_entry.Value = string.Join(TokenSep.ToString(), s_collapsed);
    }

    private static string MakeKey(string section, string group) => section + KeySep + group;
}
