using BepInEx.Logging;

namespace HeberekeBunnyGardenMod.Utils;

public static class PatchLogger
{
    private static ManualLogSource _logger;

    /// <summary>
    /// プラグインのAwakeで呼び出してLoggerを設定します
    /// </summary>
    public static void Initialize(ManualLogSource logger)
    {
        _logger = logger;
    }

    public static void LogInfo(string message)
    {
        _logger?.LogInfo($"[Plugin] {message}");
    }

    public static void LogWarning(string message)
    {
        _logger?.LogWarning($"[Plugin] {message}");
    }

    public static void LogError(string message)
    {
        _logger?.LogError($"[Plugin] {message}");
    }

    public static void LogDebug(string message)
    {
        _logger?.LogDebug($"[Plugin] {message}");
    }
}