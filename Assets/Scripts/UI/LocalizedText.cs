using TMPro;
using UnityEngine;

/// <summary>
/// TMP_Text에 붙이면 언어 변경 시 자동으로 텍스트를 갱신한다.
/// Inspector의 key에 strings.json 키를 입력하면 된다.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] string key;

    TMP_Text _text;

    void Awake()
    {
        _text = GetComponent<TMP_Text>();
        Refresh();
    }

    void OnEnable()
    {
        LanguageManager.OnLanguageChanged += OnLanguageChanged;
        Refresh();   // 패널 재활성화 시에도 현재 언어로 갱신
    }
    void OnDisable() => LanguageManager.OnLanguageChanged -= OnLanguageChanged;

    void OnLanguageChanged(LanguageManager.Language _) => Refresh();

    void Refresh()
    {
        if (_text == null) _text = GetComponent<TMP_Text>();
        string text = LocalizationManager.Get(key);
        if (!string.IsNullOrEmpty(text)) _text.text = text;
    }
}
