using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainScene");
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
