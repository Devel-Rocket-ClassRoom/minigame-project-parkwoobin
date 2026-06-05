using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설정 패널 UI 동작 담당.
/// BGM / SFX 슬라이더, 언어 드롭다운, 메인 버튼, 게임오버 버튼 처리.
/// </summary>
public class SettingPanelController : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] Slider bgmSlider;
    [SerializeField] Slider sfxSlider;

    [Header("언어")]
    [SerializeField] TMP_Dropdown languageDropdown;

    [Header("Buttons")]
    [SerializeField] Button mainButton;
    [SerializeField] Button gameOverButton;
    [SerializeField] Button helpButton;

    [Header("도움말 패널")]
    [SerializeField] TipsPanelController tipsPanel;

    void Start()
    {
        // 저장된 볼륨 값으로 슬라이더 초기화
        if (bgmSlider != null)
        {
            bgmSlider.value = PlayerPrefs.GetFloat("BGMVolume", 1f);
            bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        }
        if (sfxSlider != null)
        {
            sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
            sfxSlider.onValueChanged.AddListener(OnSfxChanged);
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

        mainButton?.onClick.AddListener(OnMainClicked);
        gameOverButton?.onClick.AddListener(OnGameOverClicked);
        helpButton?.onClick.AddListener(() => tipsPanel?.Show());
    }

    void OnLanguageChanged(int index)
    {
        var lang = index == 1 ? LanguageManager.Language.English : LanguageManager.Language.Korean;
        LanguageManager.SetLanguageStatic(lang);
    }

    void OnBgmChanged(float value)
    {
        AudioManager.Instance?.SetBgm(value);
    }

    void OnSfxChanged(float value)
    {
        AudioManager.Instance?.SetSfx(value);
    }

    void OnMainClicked()
    {
        Time.timeScale = 1f;
        GameManager.Instance?.ResumeGame();   // Paused 상태 해제
        // 진행 중이던 런 포기 — GameState 초기화해서 재시작 시 오염 방지
        if (GameState.Instance != null)
        {
            GameState.Instance.ClearTransitionEntry();
            GameState.Instance.savedMaxHP = 0;
            GameState.Instance.savedMaxHunger = 0f;
        }
        SceneTransitionManager.Instance.TransitionTo("Main");
        Debug.Log("메인 메뉴로 이동");
    }

    void OnGameOverClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        Debug.Log("게임 종료");
        GameManager.Instance?.GameOver();
    }


}
