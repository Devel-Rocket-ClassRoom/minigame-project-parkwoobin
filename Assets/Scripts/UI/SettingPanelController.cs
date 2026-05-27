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
        bgmSlider.value = PlayerPrefs.GetFloat("BGMVolume", 1f);
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);

        bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        mainButton.onClick.AddListener(OnMainClicked);
        gameOverButton.onClick.AddListener(OnGameOverClicked);
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
        // TODO: 메인 씬 로드 구현 시 여기서 처리
        // SceneManager.LoadScene("MainScene");
        Debug.Log("메인 버튼 — 미구현");
    }

    void OnGameOverClicked()
    {
        Debug.Log("게임 종료");
        GameManager.Instance?.GameOver();
    }
}
