using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점 패널. 현재 맵의 3개 업그레이드 카드를 표시한다.
/// CanvasGroup으로 숨김 처리 → 오브젝트는 항상 활성 상태.
/// </summary>
public class ShopPanel : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] Button closeButton;
    [SerializeField] Transform cardContainer;
    [SerializeField] TMP_Text coinText;
    [SerializeField] TMP_Text titleText;
    [SerializeField] GameObject dimOverlay;

    [Header("카드 프리팹")]
    [SerializeField] GameObject cardPrefab;

    public event Action OnHidden;
    public event Action OnShown;
    public bool IsVisible => canvasGroup != null && canvasGroup.alpha > 0f && canvasGroup.blocksRaycasts;

    int _lastStage = -1;

    void Awake()
    {
        if (dimOverlay == null)
        {
            Transform dim = transform.Find("DimOverlay");
            if (dim != null) dimOverlay = dim.gameObject;
        }
        ResolveTitleText();
        RefreshTitleText();

        SetVisible(false);
        closeButton?.onClick.AddListener(Hide);
    }

    void OnEnable()
    {
        CoinKeySystem.OnCoinChanged += RefreshCoin;
        LanguageManager.OnLanguageChanged += OnLanguageChanged;
    }

    void OnDisable()
    {
        CoinKeySystem.OnCoinChanged -= RefreshCoin;
        LanguageManager.OnLanguageChanged -= OnLanguageChanged;
    }

    void SetVisible(bool v)
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = v ? 1f : 0f;
        canvasGroup.interactable = v;
        canvasGroup.blocksRaycasts = v;

        if (dimOverlay != null)
            dimOverlay.SetActive(v);
    }

    public void Show()
    {
        SetVisible(true);
        Time.timeScale = 0f;
        RefreshTitleText();
        RefreshCoin(CoinKeySystem.Instance != null ? CoinKeySystem.Instance.Coins : 0);
        // 열 때마다 스킬 상태 반영해 카드 재생성
        UpgradeManager.Instance?.InvalidateOffers();
        _lastStage = -1;
        BuildCards();
        OnShown?.Invoke();
    }

    public void Hide()
    {
        SetVisible(false);
        Time.timeScale = 1f;
        OnHidden?.Invoke();
    }

    void RefreshCoin(int coins)
    {
        if (coinText != null) coinText.text = $"{coins}";
    }

    void OnLanguageChanged(LanguageManager.Language _)
    {
        RefreshTitleText();
        RefreshCards();
    }

    void RefreshTitleText()
    {
        ResolveTitleText();
        string text = LocalizationManager.Get("menu_shop");
        if (titleText != null && !string.IsNullOrEmpty(text))
            titleText.text = text;
    }

    void RefreshCards()
    {
        if (cardContainer == null) return;
        foreach (var card in cardContainer.GetComponentsInChildren<UpgradeCardUI>(true))
            card.RefreshLocalizedText();
    }

    void ResolveTitleText()
    {
        if (titleText != null) return;

        Transform title = transform.Find("Title");
        if (title == null) title = transform.Find("PopupPanel/Header/Title");
        if (title == null) title = transform.Find("Header/Title");
        if (title != null) titleText = title.GetComponent<TMP_Text>();
    }

    void BuildCards()
    {
        if (cardContainer == null) return;

        int stage = GameState.Instance != null ? GameState.Instance.savedStage : 0;
        if (stage == _lastStage && cardContainer.childCount == 3) return;

        foreach (Transform child in cardContainer)
            Destroy(child.gameObject);

        EnsureUpgradeManager();
        if (UpgradeManager.Instance == null || cardPrefab == null) return;

        var offers = UpgradeManager.Instance.GetOffers();
        foreach (var offer in offers)
        {
            var go = Instantiate(cardPrefab, cardContainer);
            var card = go.GetComponent<UpgradeCardUI>();
            card?.Setup(offer);
        }

        _lastStage = stage;
    }

    void EnsureUpgradeManager()
    {
        if (UpgradeManager.Instance != null) return;

        var go = new GameObject(nameof(UpgradeManager));
        go.AddComponent<UpgradeManager>();
    }
}
