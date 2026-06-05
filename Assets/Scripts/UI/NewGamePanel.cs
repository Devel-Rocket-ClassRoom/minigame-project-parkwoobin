using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 새 게임 슬롯 선택 패널.
/// 빈 슬롯만 선택 가능. 데이터 있는 슬롯은 비활성화.
/// Delete 토글 → X버튼으로 기존 데이터 삭제 가능.
/// </summary>
public class NewGamePanel : MonoBehaviour
{
    [Header("패널 루트")]
    [SerializeField] GameObject panelRoot;
    [SerializeField] GameObject DimOverlay;

    [Header("슬롯")]
    [SerializeField] Button[] slotButtons;        // 슬롯 0, 1, 2 — 빈 슬롯 선택
    [SerializeField] TextMeshProUGUI[] slotTexts; // 슬롯 0, 1, 2 — 라벨
    [SerializeField] Button[] xButtons;           // 슬롯 0, 1, 2 — X 삭제 버튼

    [Header("버튼")]
    [SerializeField] Button deleteToggleButton;
    [SerializeField] Button closeButton;

    const string START_SCENE = "Prologue";

    bool _deleteMode;

    void OnEnable()  => LanguageManager.OnLanguageChanged += OnLanguageChanged;
    void OnDisable() => LanguageManager.OnLanguageChanged -= OnLanguageChanged;
    void OnLanguageChanged(LanguageManager.Language _) { if (panelRoot != null && panelRoot.activeSelf) RefreshSlots(); }

    void Start()
    {
        if (deleteToggleButton != null) deleteToggleButton.onClick.AddListener(OnDeleteToggle);
        if (closeButton != null)        closeButton.onClick.AddListener(Close);

        for (int i = 0; i < slotButtons.Length; i++)
        {
            int slot = i;
            if (slotButtons[i] != null)
                slotButtons[i].onClick.AddListener(() => OnSlotSelected(slot));
        }

        for (int i = 0; i < xButtons.Length; i++)
        {
            int slot = i;
            if (xButtons[i] != null)
                xButtons[i].onClick.AddListener(() => OnSlotDeleted(slot));
        }

        if (panelRoot != null) panelRoot.SetActive(false);
    }

    // ── 외부 호출 ─────────────────────────────────────────────────────────────

    public void Open()
    {
        _deleteMode = false;
        SetXButtonsVisible(false);
        RefreshSlots();
        if (panelRoot != null) panelRoot.SetActive(true);
        if (DimOverlay != null) DimOverlay.SetActive(true);
    }

    public void Close()
    {
        _deleteMode = false;
        SetXButtonsVisible(false);
        if (panelRoot != null) panelRoot.SetActive(false);
        if (DimOverlay != null) DimOverlay.SetActive(false);
    }

    // ── 내부 동작 ─────────────────────────────────────────────────────────────

    void OnDeleteToggle()
    {
        _deleteMode = !_deleteMode;
        SetXButtonsVisible(_deleteMode);
    }

    void SetXButtonsVisible(bool visible)
    {
        if (xButtons == null) return;
        for (int i = 0; i < xButtons.Length; i++)
        {
            if (xButtons[i] == null) continue;
            bool hasData = SaveManager.Instance != null && SaveManager.Instance.HasSaveData(i);
            // 데이터 있는 슬롯에만 X버튼 표시
            xButtons[i].gameObject.SetActive(visible && hasData);
        }
    }

    void RefreshSlots()
    {
        for (int i = 0; i < 3; i++)
        {
            bool hasData = SaveManager.Instance != null && SaveManager.Instance.HasSaveData(i);

            if (slotTexts != null && i < slotTexts.Length && slotTexts[i] != null)
            {
                string slotLabel = LocalizationManager.Get("menu_slot");
                if (hasData)
                {
                    var data = SaveManager.Instance.LoadGame(i);
                    slotTexts[i].text = $"{slotLabel} {i + 1}\n{data.sceneName}";
                }
                else
                {
                    slotTexts[i].text = $"{slotLabel} {i + 1}\n{LocalizationManager.Get("menu_slot_empty")}";
                }
            }

            // 빈 슬롯만 선택 가능
            if (slotButtons != null && i < slotButtons.Length && slotButtons[i] != null)
                slotButtons[i].interactable = !hasData;
        }

        // Delete 버튼: 데이터가 하나도 없으면 비활성화
        if (deleteToggleButton != null)
            deleteToggleButton.interactable = HasAnyData();
    }

    void OnSlotSelected(int slot)
    {
        if (_deleteMode) return;

        Close();

        if (GameState.Instance != null)
        {
            GameState.Instance.ClearTransitionEntry();
            GameState.Instance.savedMaxHP = 0;
            GameState.Instance.savedMaxHunger = 0f;
            GameState.Instance.hasSavedPosition = false;
        }

        GameManager.Instance?.StartGame();
        SaveManager.Instance?.StartNewGame(slot);
        SceneTransitionManager.Instance.TransitionTo(START_SCENE);
    }

    void OnSlotDeleted(int slot)
    {
        SaveManager.Instance?.DeleteSave(slot);
        RefreshSlots();
        SetXButtonsVisible(_deleteMode);

        // 모두 지워졌으면 삭제 모드 종료
        if (!HasAnyData())
        {
            _deleteMode = false;
            SetXButtonsVisible(false);
        }
    }

    bool HasAnyData()
    {
        for (int i = 0; i < 3; i++)
            if (SaveManager.Instance != null && SaveManager.Instance.HasSaveData(i))
                return true;
        return false;
    }
}
