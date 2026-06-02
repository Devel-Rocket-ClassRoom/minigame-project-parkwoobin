using Unity.VisualScripting;
using UnityEngine;

using UnityEngine.UI;

/// <summary>
/// 설정 패널 UI 동작 담당.
/// BGM / SFX 슬라이더, 메인 버튼, 게임오버 버튼 처리.
/// </summary>
public class SettingPanelController : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] Slider bgmSlider;
    [SerializeField] Slider sfxSlider;

    [Header("Buttons")]
    [SerializeField] Button mainButton;
    [SerializeField] Button gameOverButton;

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
        mainButton?.onClick.AddListener(OnMainClicked);
        gameOverButton?.onClick.AddListener(OnGameOverClicked);
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
