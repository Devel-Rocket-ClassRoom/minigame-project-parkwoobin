using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 메인 메뉴 화면 관리 — 새 게임 / 게임 불러오기 / 게임 종료
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("메뉴 버튼")]
    [SerializeField] Button newGameButton;
    [SerializeField] Button loadGameButton;
    [SerializeField] Button quitButton;

    [Header("저장 데이터 없음 팝업")]
    [SerializeField] GameObject noSaveDataPopup;
    [SerializeField] GameObject DimOverlay;
    [SerializeField] Button confirmPopupButton;

    [Header("씬 설정")]
    [SerializeField] string gameSceneName = "SampleScene";

    void Start()
    {
        if (newGameButton != null) newGameButton.onClick.AddListener(OnNewGameClick);
        if (loadGameButton != null) loadGameButton.onClick.AddListener(OnLoadGameClick);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitClick);
        if (confirmPopupButton != null) confirmPopupButton.onClick.AddListener(OnConfirmPopupClick);

        if (noSaveDataPopup != null) noSaveDataPopup.SetActive(false);
    }

    void OnNewGameClick()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    void OnLoadGameClick()
    {
        if (SaveManager.Instance == null || !SaveManager.Instance.HasSaveData())
        {
            ShowNoSaveDataPopup();
            return;
        }
        SceneManager.LoadScene(gameSceneName);
    }

    void OnQuitClick()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void ShowNoSaveDataPopup()
    {
        if (noSaveDataPopup != null) noSaveDataPopup.SetActive(true);
        if (DimOverlay != null) DimOverlay.SetActive(true);
    }

    void OnConfirmPopupClick()
    {
        if (noSaveDataPopup != null) noSaveDataPopup.SetActive(false);
        if (DimOverlay != null) DimOverlay.SetActive(false);
    }
}
