using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resources/Localization/strings.json 을 로드해 키→현재 언어 문자열을 반환한다.
/// JSON 포맷: { "key": { "ko": "...", "en": "..." }, ... }
/// 언어 추가 시 JSON에 새 코드를 추가하고 LanguageManager.Language enum을 확장하면 된다.
/// </summary>
public class LocalizationManager : MonoBehaviour
{
    const string ResourcePath = "Localization/strings";

    public static LocalizationManager Instance { get; private set; }

    // key → (langCode → text) — static이라 Instance가 null이어도 유지됨
    static Dictionary<string, Dictionary<string, string>> _table;

    static void Load()
    {
        _table = new Dictionary<string, Dictionary<string, string>>();

        var asset = Resources.Load<TextAsset>(ResourcePath);
        if (asset == null)
        {
            Debug.LogWarning("[LocalizationManager] strings.json 을 찾을 수 없습니다: " + ResourcePath);
            return;
        }

        ParseJson(asset.text);
        Debug.Log($"[LocalizationManager] {_table.Count}개 키 로드 완료");
    }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        if (_table == null) Load();
    }

    /// <summary>현재 언어로 키에 해당하는 문자열을 반환한다. 없으면 빈 문자열 반환.</summary>
    public static string Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        // Instance 없어도 동작 — 씬 직접 플레이 시 자동 로드
        if (_table == null) Load();
        if (_table == null) return "";

        // static 언어 상태 사용 — 인스턴스 생존과 무관하게 항상 올바른 언어
        string langCode = LanguageManager.CurrentLanguage == LanguageManager.Language.English
                          ? "en" : "ko";

        if (_table.TryGetValue(key, out var langs))
        {
            if (langs.TryGetValue(langCode, out var text)) return text;
            // 요청 언어 없으면 첫 번째 값으로 폴백
            foreach (var v in langs.Values) return v;
        }

        Debug.LogWarning($"[LocalizationManager] 키 없음: '{key}'");
        return "";
    }

    // ── 경량 JSON 파서 ────────────────────────────────────────────────────────
    // Unity JsonUtility는 Dictionary를 지원하지 않으므로 직접 파싱한다.
    // 지원 포맷: { "key": { "code": "value", ... }, ... }
    // 값 문자열 내 이스케이프 시퀀스: \" \\ \n \t
    static void ParseJson(string json)
    {
        int i = 0;
        SkipWhitespace(json, ref i);
        if (i >= json.Length || json[i] != '{') return;
        i++; // '{'

        while (i < json.Length)
        {
            SkipWhitespace(json, ref i);
            if (i >= json.Length || json[i] == '}') break;
            if (json[i] == ',') { i++; continue; }

            // 외부 키
            string outerKey = ReadString(json, ref i);
            if (outerKey == null) break;

            SkipWhitespace(json, ref i);
            if (i >= json.Length || json[i] != ':') break;
            i++; // ':'

            SkipWhitespace(json, ref i);

            // "_comment" 같은 비-객체 값은 건너뜀
            if (i < json.Length && json[i] != '{') { SkipValue(json, ref i); continue; }

            // 내부 객체 { "code": "text", ... }
            var langs = new Dictionary<string, string>();
            i++; // '{'
            while (i < json.Length)
            {
                SkipWhitespace(json, ref i);
                if (i >= json.Length || json[i] == '}') { i++; break; }
                if (json[i] == ',') { i++; continue; }

                string langCode = ReadString(json, ref i);
                if (langCode == null) break;
                SkipWhitespace(json, ref i);
                if (i >= json.Length || json[i] != ':') break;
                i++; // ':'
                SkipWhitespace(json, ref i);
                string value = ReadString(json, ref i);
                if (value != null) langs[langCode] = value;
            }

            _table[outerKey] = langs;
        }
    }

    static void SkipWhitespace(string s, ref int i)
    {
        while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\r' || s[i] == '\n')) i++;
    }

    static string ReadString(string s, ref int i)
    {
        SkipWhitespace(s, ref i);
        if (i >= s.Length || s[i] != '"') return null;
        i++; // opening "
        var sb = new System.Text.StringBuilder();
        while (i < s.Length && s[i] != '"')
        {
            if (s[i] == '\\' && i + 1 < s.Length)
            {
                i++;
                switch (s[i])
                {
                    case '"':  sb.Append('"');  break;
                    case '\\': sb.Append('\\'); break;
                    case 'n':  sb.Append('\n'); break;
                    case 't':  sb.Append('\t'); break;
                    default:   sb.Append(s[i]); break;
                }
            }
            else sb.Append(s[i]);
            i++;
        }
        if (i < s.Length) i++; // closing "
        return sb.ToString();
    }

    // 문자열·숫자·null·배열·객체 등 임의의 JSON 값을 건너뜀
    static void SkipValue(string s, ref int i)
    {
        SkipWhitespace(s, ref i);
        if (i >= s.Length) return;
        if (s[i] == '"') { ReadString(s, ref i); return; }
        if (s[i] == '{' || s[i] == '[')
        {
            char open = s[i], close = s[i] == '{' ? '}' : ']';
            int depth = 1; i++;
            while (i < s.Length && depth > 0)
            {
                if (s[i] == '"') ReadString(s, ref i);
                else { if (s[i] == open) depth++; else if (s[i] == close) depth--; i++; }
            }
            return;
        }
        while (i < s.Length && s[i] != ',' && s[i] != '}' && s[i] != ']') i++;
    }
}
