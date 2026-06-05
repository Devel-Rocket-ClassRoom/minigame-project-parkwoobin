using UnityEngine;
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
    [SerializeField] Button helpButton;

    [Header("도움말 패널")]
    [SerializeField] MainMenuTipsPanel tipsPanel;

    [Header("저장 데이터 없음 팝업")]
    [SerializeField] GameObject noSaveDataPopup;
    [SerializeField] GameObject DimOverlay;
    [SerializeField] Button confirmPopupButton;

    [Header("슬롯 선택 패널")]
    [SerializeField] SaveSlotPanel saveSlotPanel;
    [SerializeField] NewGamePanel newGamePanel;

    [Header("BGM")]
    [SerializeField] AudioClip mainBgm;

    void Start()
    {
        if (newGameButton != null)      newGameButton.onClick.AddListener(OnNewGameClick);
        if (loadGameButton != null)     loadGameButton.onClick.AddListener(OnLoadGameClick);
        if (quitButton != null)         quitButton.onClick.AddListener(OnQuitClick);
        if (confirmPopupButton != null) confirmPopupButton.onClick.AddListener(OnConfirmPopupClick);
        helpButton?.onClick.AddListener(() => tipsPanel?.Show());

        if (noSaveDataPopup != null) noSaveDataPopup.SetActive(false);

        // Main 씬 BGM 재생
        if (mainBgm != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayBgm(mainBgm);
    }

    void OnNewGameClick()
    {
        if (newGamePanel != null)
            newGamePanel.Open();
    }

    void OnLoadGameClick()
    {
        if (saveSlotPanel == null || !saveSlotPanel.HasAnySaveData())
        {
            ShowNoSaveDataPopup();
            return;
        }
        saveSlotPanel.Open();
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
        if (DimOverlay != null)      DimOverlay.SetActive(true);
    }

    void OnConfirmPopupClick()
    {
        if (noSaveDataPopup != null) noSaveDataPopup.SetActive(false);
        if (DimOverlay != null)      DimOverlay.SetActive(false);
    }
}
