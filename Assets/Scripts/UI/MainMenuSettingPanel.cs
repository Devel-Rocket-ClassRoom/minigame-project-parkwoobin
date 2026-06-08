using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 메인화면 세팅 패널.
/// BGM / SFX 볼륨 슬라이더 + 언어 드롭다운(한국어 / English).
/// </summary>
public class MainMenuSettingPanel : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] GameObject panelRoot;
    [SerializeField] GameObject dimOverlay;  // 패널 뒤 어두운 배경
    [SerializeField] Button openButton;    // 세팅 버튼
    [SerializeField] Button closeButton;   // 닫기 버튼

    [Header("볼륨")]
    [SerializeField] Slider bgmSlider;
    [SerializeField] Slider sfxSlider;

    [Header("언어")]
    [SerializeField] TMP_Dropdown languageDropdown;

    void Start()
    {
        // 패널 기본 숨김
        if (panelRoot != null) panelRoot.SetActive(false);
        if (dimOverlay != null) dimOverlay.SetActive(false);

        openButton?.onClick.AddListener(Open);
        closeButton?.onClick.AddListener(Close);

        // 볼륨 슬라이더 초기화
        if (bgmSlider != null)
        {
            bgmSlider.value = PlayerPrefs.GetFloat("BGMVolume", 1f);
            bgmSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetBgm(v));
        }
        if (sfxSlider != null)
        {
            sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
            sfxSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetSfx(v));
        }

        // 언어 드롭다운 초기화
        if (languageDropdown != null)
        {
            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(new System.Collections.Generic.List<string> { "한국어", "English" });

            int idx = LanguageManager.CurrentLanguage == LanguageManager.Language.English ? 1 : 0;
            languageDropdown.SetValueWithoutNotify(idx);
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        }
    }

    public void Open()
    {
        if (dimOverlay != null) dimOverlay.SetActive(true);
        if (panelRoot != null) panelRoot.SetActive(true);
        RefreshLanguageDropdown();
    }

    void RefreshLanguageDropdown()
    {
        if (languageDropdown == null) return;
        int idx = LanguageManager.CurrentLanguage == LanguageManager.Language.English ? 1 : 0;
        languageDropdown.SetValueWithoutNotify(idx);
        languageDropdown.RefreshShownValue();
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (dimOverlay != null) dimOverlay.SetActive(false);
    }

    void OnLanguageChanged(int index)
    {
        var lang = index == 1 ? LanguageManager.Language.English : LanguageManager.Language.Korean;
        LanguageManager.SetLanguageStatic(lang);
    }
}
