using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using GB;
using GB.Game;

namespace HeberekeBunnyGardenMod.Utils;

/// <summary>
/// MOD UI 文字列の多言語化（issue #52）。
///
/// ■ 方式
///   日本語テキストをそのままキーにし、現在のゲーム言語に対応する訳文へ置換する。
///   <c>Loc.Tr("日本語テキスト")</c> が翻訳を返し、訳が無ければ日本語をそのまま返す（フォールバック）。
///   これにより既存のハードコード文字列を <c>Loc.Tr(...)</c> で包むだけで多言語化でき、
///   キー管理の手間や未訳時の表示崩れが起きない。
///
/// ■ 言語ファイル
///   言語コードごとに「日本語 → 訳文」の JSON 辞書を持つ。読み込み優先順位:
///     1. 外部ファイル <c>&lt;pluginDir&gt;/lang/&lt;code&gt;.json</c>（翻訳者がビルド不要で編集・追加可能）
///     2. 埋め込みリソース <c>HeberekeBunnyGardenMod.Resources.lang.&lt;code&gt;.json</c>（DLL 同梱の既定訳）
///   日本語選択時は辞書を読まず素通し。コードは <see cref="GB.Save.Language"/> に対応
///   （English→en / Chinese_zhCN→zhCN / Chinese_zhtw→zhtw）。将来 ko 等を足す場合は
///   lang/ko.json を置けば <see cref="ResolveCode"/> の対応追加だけで有効化できる。
///
/// ■ 言語切替
///   ゲーム言語は実行中に変わりうるため、F9 パネルや CostumePicker を開くタイミングで
///   <see cref="Refresh"/> を呼んで再解決する。辞書はコード単位でキャッシュする。
/// </summary>
public static class Loc
{
    private static readonly Dictionary<string, Dictionary<string, string>> s_cacheByCode = new();
    private static string s_currentCode = "ja";
    private static Dictionary<string, string> s_current; // null = 日本語（素通し）

    /// <summary>現在解決済みの言語コード（ja/en/zhCN/zhtw）。一度構築した UI の言語変更検出に使う。</summary>
    public static string CurrentCode => s_currentCode;

    /// <summary>
    /// 現在のゲーム言語を読み直し、対応する辞書を選び直す。F9/Picker を開くときに呼ぶ。冪等。
    /// </summary>
    public static void Refresh()
    {
        var code = ResolveCode();
        if (code == s_currentCode && (code == "ja" || s_current != null)) return;

        s_currentCode = code;
        s_current = code == "ja" ? null : LoadOrGet(code);
    }

    /// <summary>日本語テキストを現在言語の訳文へ変換する。訳が無ければ入力をそのまま返す。</summary>
    public static string Tr(string ja)
    {
        if (string.IsNullOrEmpty(ja) || s_current == null) return ja;
        // 完全一致を優先。説明文の末尾改行など細かな差を吸収するため、末尾空白を除いた一致もフォールバックする。
        if (s_current.TryGetValue(ja, out var v) && !string.IsNullOrEmpty(v)) return v;
        var trimmed = ja.TrimEnd();
        if (trimmed.Length != ja.Length
            && s_current.TryGetValue(trimmed, out var v2) && !string.IsNullOrEmpty(v2))
            return v2;
        return ja;
    }

    private static string ResolveCode()
    {
        try
        {
            // へべれけ版は言語を GBSystem.SystemSetting.Language (SystemData.Settings.Languages) で保持する。
            var setting = GBSystem.SystemSetting;
            if (setting == null) return s_currentCode; // 未ロード時は現状維持
            return setting.Language switch
            {
                SystemData.Settings.Languages.En => "en",
                SystemData.Settings.Languages.ZhCn => "zhCN",
                SystemData.Settings.Languages.ZhTw => "zhtw",
                _ => "ja",
            };
        }
        catch
        {
            return s_currentCode;
        }
    }

    private static Dictionary<string, string> LoadOrGet(string code)
    {
        if (s_cacheByCode.TryGetValue(code, out var cached)) return cached;

        Dictionary<string, string> map = null;
        try
        {
            // 1. 外部ファイル優先
            var ext = ExternalPath(code);
            if (ext != null && File.Exists(ext))
            {
                map = ParseFlatJson(File.ReadAllText(ext, Encoding.UTF8));
                PatchLogger.LogInfo($"[Loc] 外部言語ファイルを読み込み: {ext} ({map.Count} 件)");
            }
            else
            {
                // 2. 埋め込みリソース
                var asm = Assembly.GetExecutingAssembly();
                var resName = $"HeberekeBunnyGardenMod.Resources.lang.{code}.json";
                using var stream = asm.GetManifestResourceStream(resName);
                if (stream != null)
                {
                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    map = ParseFlatJson(reader.ReadToEnd());
                    PatchLogger.LogInfo($"[Loc] 埋め込み言語リソースを読み込み: {code} ({map.Count} 件)");
                }
                else
                {
                    PatchLogger.LogInfo($"[Loc] 言語 '{code}' の辞書が見つかりません。日本語で表示します。");
                }
            }
        }
        catch (Exception e)
        {
            PatchLogger.LogWarning($"[Loc] 言語 '{code}' の読み込み失敗: {e.Message}");
        }

        map ??= new Dictionary<string, string>();
        s_cacheByCode[code] = map;
        return map;
    }

    private static string ExternalPath(string code)
    {
        try
        {
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return dir == null ? null : Path.Combine(dir, "lang", code + ".json");
        }
        catch { return null; }
    }

    // ---- 最小 JSON パーサ（フラットな { "key": "value", ... } を string→string に） ----
    // 値は文字列のみ想定。標準のエスケープ（\" \\ \/ \n \r \t \b \f \uXXXX）に対応する。
    private static Dictionary<string, string> ParseFlatJson(string text)
    {
        var dict = new Dictionary<string, string>();
        int i = 0, n = text.Length;
        SkipWs(text, ref i);
        if (i >= n || text[i] != '{') return dict;
        i++;
        while (true)
        {
            SkipWs(text, ref i);
            if (i >= n) break;
            if (text[i] == '}') { i++; break; }
            if (text[i] == ',') { i++; continue; }
            if (text[i] != '"') { i++; continue; }

            string key = ReadString(text, ref i);
            SkipWs(text, ref i);
            if (i < n && text[i] == ':') i++;
            SkipWs(text, ref i);
            if (i < n && text[i] == '"')
            {
                string val = ReadString(text, ref i);
                if (key != null) dict[key] = val;
            }
        }
        return dict;
    }

    private static void SkipWs(string s, ref int i)
    {
        while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\r' || s[i] == '\n')) i++;
    }

    private static string ReadString(string s, ref int i)
    {
        // s[i] == '"'
        i++;
        var sb = new StringBuilder();
        while (i < s.Length)
        {
            char c = s[i++];
            if (c == '"') break;
            if (c == '\\' && i < s.Length)
            {
                char e = s[i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'u':
                        if (i + 4 <= s.Length &&
                            int.TryParse(s.Substring(i, 4), System.Globalization.NumberStyles.HexNumber,
                                System.Globalization.CultureInfo.InvariantCulture, out int cp))
                        {
                            sb.Append((char)cp);
                            i += 4;
                        }
                        break;
                    default: sb.Append(e); break;
                }
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}
