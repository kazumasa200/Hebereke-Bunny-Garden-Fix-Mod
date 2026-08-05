using System.Collections.Generic;
using BepInEx.Configuration;
using HeberekeBunnyGardenMod.Utils;
using HarmonyLib;

namespace HeberekeBunnyGardenMod;

/// <summary>
/// 旧 .cfg レイアウトから現行スキーマへの移行。
/// へべれけ版は既存の section/key 名（Resolution / AntiAliasing / Censor / Cheat / Camera）を
/// そのまま踏襲するため、現時点でリネーム移行は不要。機構のみ残し、将来 section/key を
/// 再編したくなったら <see cref="Migrations"/> / <see cref="ObsoleteKeys"/> に1行追加すればよい。
/// </summary>
public static class ConfigMigration
{
    private readonly struct MigrationEntry(ConfigDefinition oldDef, ConfigDefinition newDef)
    {
        public ConfigDefinition OldDef { get; } = oldDef;
        public ConfigDefinition NewDef { get; } = newDef;
    }

    /// <summary>廃止されたキー（移行先なし・削除のみ）。</summary>
    private static readonly ConfigDefinition[] ObsoleteKeys = [];

    /// <summary>旧キー → 新キー のリネーム移行（配列順に適用）。</summary>
    private static readonly MigrationEntry[] Migrations = [];

    public static void Migrate(ConfigFile config)
    {
        if (Migrations.Length == 0 && ObsoleteKeys.Length == 0)
            return; // 移行不要（新規追加キーは BindAll が既定値で bind する）

        PatchLogger.LogInfo($"[{nameof(ConfigMigration)}] 設定の移行を開始します");
        var previousSaveOnConfigSet = config.SaveOnConfigSet;
        config.SaveOnConfigSet = false;

        try
        {
            var orphanedEntries = GetOrphanedEntries(config);
            if (orphanedEntries == null)
            {
                PatchLogger.LogWarning(
                    $"[{nameof(ConfigMigration)}] OrphanedEntries にアクセスできませんでした。移行はスキップされます");
                return;
            }

            ArrayMigration(orphanedEntries);
            RemoveObsoleteKeys(orphanedEntries);

            config.Save();
            PatchLogger.LogInfo($"[{nameof(ConfigMigration)}] 設定の移行が完了しました");
        }
        finally
        {
            config.SaveOnConfigSet = previousSaveOnConfigSet;
        }
    }

    private static Dictionary<ConfigDefinition, string> GetOrphanedEntries(ConfigFile config) =>
        (Dictionary<ConfigDefinition, string>)
        AccessTools.PropertyGetter(typeof(ConfigFile), "OrphanedEntries").Invoke(config, null);

    private static void RemoveObsoleteKeys(Dictionary<ConfigDefinition, string> orphanedEntries)
    {
        foreach (var key in ObsoleteKeys)
        {
            if (!orphanedEntries.Remove(key)) continue;
            PatchLogger.LogInfo($"[{nameof(ConfigMigration)}] 廃止された設定エントリを削除しました: {key}");
        }
    }

    private static void ArrayMigration(Dictionary<ConfigDefinition, string> orphanedEntries)
    {
        foreach (var entry in Migrations)
        {
            if (!orphanedEntries.TryGetValue(entry.OldDef, out var oldValue))
                continue;

            orphanedEntries.Remove(entry.OldDef);
            PatchLogger.LogInfo($"[{nameof(ConfigMigration)}] 古い設定エントリを削除しました: {entry.OldDef}");

            if (orphanedEntries.ContainsKey(entry.NewDef))
                continue;

            orphanedEntries.Add(entry.NewDef, oldValue);
            PatchLogger.LogInfo(
                $"[{nameof(ConfigMigration)}] 新しい設定エントリに移行しました: {entry.NewDef} = {oldValue}");
        }
    }
}
