using UnityEngine;
using System;

/// <summary>
/// 언어 설정을 관리하는 싱글톤.
/// PlayerPrefs "Language" 키에 "ko" / "en" 저장.
/// 언어 변경 시 OnLanguageChanged 이벤트 발행.
/// </summary>
public class LanguageManager : MonoBehaviour
{
    public enum Language { Korean, English }

    public static LanguageManager Instance { get; private set; }

    public static event Action<Language> OnLanguageChanged;

    const string KEY = "Language";

    public Language Current { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        string saved = PlayerPrefs.GetString(KEY, "ko");
        Current = saved == "en" ? Language.English : Language.Korean;
    }

    public void SetLanguage(Language lang)
    {
        Current = lang;
        PlayerPrefs.SetString(KEY, lang == Language.English ? "en" : "ko");
        PlayerPrefs.Save();
        OnLanguageChanged?.Invoke(lang);
    }
}
