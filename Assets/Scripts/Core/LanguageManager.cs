using UnityEngine;
using System;

/// <summary>
/// 언어 설정을 관리한다.
/// 상태는 static + PlayerPrefs("Language") 단일 출처로 보관하므로
/// 씬 전환·인스턴스 파괴와 무관하게 항상 올바른 언어를 반환한다.
/// </summary>
public class LanguageManager : MonoBehaviour
{
    public enum Language { Korean, English }

    public static LanguageManager Instance { get; private set; }
    public static event Action<Language> OnLanguageChanged;

    const string KEY = "Language";

    static bool _loaded;
    static Language _current;

    /// <summary>인스턴스 없이도 동작하는 static 현재 언어.</summary>
    public static Language CurrentLanguage
    {
        get
        {
            if (!_loaded)
            {
                string saved = PlayerPrefs.GetString(KEY, "ko");
                _current = saved == "en" ? Language.English : Language.Korean;
                _loaded = true;
            }
            return _current;
        }
    }

    /// <summary>기존 호환용 인스턴스 속성 — static 값을 그대로 반환.</summary>
    public Language Current => CurrentLanguage;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        // static 캐시 강제 로드
        _loaded = false;
        var _ = CurrentLanguage;
    }

    public void SetLanguage(Language lang) => SetLanguageStatic(lang);

    /// <summary>인스턴스 없이도 호출 가능한 static 언어 변경.</summary>
    public static void SetLanguageStatic(Language lang)
    {
        _current = lang;
        _loaded = true;
        PlayerPrefs.SetString(KEY, lang == Language.English ? "en" : "ko");
        PlayerPrefs.Save();
        OnLanguageChanged?.Invoke(lang);
    }
}
