using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 세이브 슬롯 선택 패널 — 불러오기 / 삭제 토글 / X버튼 삭제
/// MainMenuManager 등 외부에서 Open() 호출로 표시한다.
/// </summary>
public class SaveSlotPanel : MonoBehaviour
{
    [Header("패널 루트")]
    [SerializeField] GameObject panelRoot;
    [SerializeField] GameObject DimOverlay;

    [Header("슬롯")]
    [SerializeField] Button[] slotButtons;        // 슬롯 0, 1, 2 — 불러오기
    [SerializeField] TextMeshProUGUI[] slotTexts; // 슬롯 0, 1, 2 — 라벨
    [SerializeField] Button[] xButtons;           // 슬롯 0, 1, 2 — X 삭제 버튼

    [Header("버튼")]
    [SerializeField] Button deleteToggleButton;
    [SerializeField] Button closeButton;

    bool _deleteMode;

    void OnEnable()  => LanguageManager.OnLanguageChanged += OnLanguageChanged;
    void OnDisable() => LanguageManager.OnLanguageChanged -= OnLanguageChanged;
    void OnLanguageChanged(LanguageManager.Language _) { if (panelRoot != null && panelRoot.activeSelf) RefreshSlotTexts(); }

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
        RefreshSlotTexts();
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

    public bool HasAnySaveData()
    {
        for (int i = 0; i < 3; i++)
            if (SaveManager.Instance != null && SaveManager.Instance.HasSaveData(i))
                return true;
        return false;
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
            xButtons[i].gameObject.SetActive(visible && hasData);
        }
    }

    void RefreshSlotTexts()
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

            if (slotButtons != null && i < slotButtons.Length && slotButtons[i] != null)
                slotButtons[i].interactable = hasData;
        }
    }

    void OnSlotSelected(int slot)
    {
        if (_deleteMode) return;
        Close();
        GameManager.Instance?.StartGame();
        SaveManager.Instance?.LoadAndApply(slot);
    }

    void OnSlotDeleted(int slot)
    {
        SaveManager.Instance?.DeleteSave(slot);
        RefreshSlotTexts();
        SetXButtonsVisible(_deleteMode);

        if (!HasAnySaveData()) Close();
    }
}
