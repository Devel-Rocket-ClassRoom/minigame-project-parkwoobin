using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점 업그레이드 카드 1장. ShopPanel이 동적으로 생성해 데이터를 주입한다.
/// </summary>
public class UpgradeCardUI : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text descText;
    [SerializeField] TMP_Text costText;
    [SerializeField] TMP_Text levelText;
    [SerializeField] Button buyButton;
    [SerializeField] TMP_Text buyButtonText;
    [SerializeField] Image cardBackground;
    [SerializeField] Image iconImage;

    [Header("아이콘")]
    [SerializeField] Sprite speedIcon;
    [SerializeField] Sprite dashSpeedIcon;
    [SerializeField] Sprite jumpHeightIcon;
    [SerializeField] Sprite HpIcon;
    [SerializeField] Sprite HungerIcon;
    [SerializeField] Sprite dashCooldownIcon;
    [SerializeField] Sprite turnCooldownIcon;

    [Header("색상")]
    [SerializeField] Color colorAvailable = new Color(0.2f, 0.8f, 0.3f);
    [SerializeField] Color colorMaxLevel = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] Color colorNoCoin = new Color(0.8f, 0.3f, 0.2f);

    UpgradeType _type;

    public void Setup(UpgradeType type)
    {
        _type = type;
        DisableNonButtonRaycasts();
        Refresh();
        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(OnBuyClicked);
            buyButton.onClick.AddListener(OnBuyClicked);
        }
        UpgradeManager.OnUpgraded += OnUpgraded;
        CoinKeySystem.OnCoinChanged += OnCoinChanged;
        LanguageManager.OnLanguageChanged += OnLanguageChanged;
    }

    void OnDestroy()
    {
        UpgradeManager.OnUpgraded -= OnUpgraded;
        CoinKeySystem.OnCoinChanged -= OnCoinChanged;
        LanguageManager.OnLanguageChanged -= OnLanguageChanged;
    }

    void OnUpgraded(UpgradeType t, int _)
    {
        if (t == _type) Refresh();
        Refresh(); // 코인 변경으로 다른 카드도 버튼 상태 갱신
    }

    void OnCoinChanged(int _) => Refresh();

    void OnLanguageChanged(LanguageManager.Language _) => Refresh();

    public void RefreshLocalizedText() => Refresh();

    void DisableNonButtonRaycasts()
    {
        foreach (var graphic in GetComponentsInChildren<Graphic>(true))
        {
            if (buyButton != null && graphic.GetComponentInParent<Button>() == buyButton)
                continue;
            graphic.raycastTarget = false;
        }
    }

    void Refresh()
    {
        if (UpgradeManager.Instance == null) return;
        int level = UpgradeManager.Instance.GetLevel(_type);
        int cost = UpgradeManager.Instance.GetCost(_type);
        bool maxed = level >= UpgradeManager.MaxLevel;
        bool canBuy = UpgradeManager.Instance.CanBuy(_type);

        RefreshIcon();
        RefreshLevel(level);

        // 이름·설명
        if (nameText != null) nameText.text = LocalizationManager.Get($"upgrade_{_type.ToString().ToLower()}_name");
        if (descText != null) descText.text = LocalizationManager.Get($"upgrade_{_type.ToString().ToLower()}_desc");

        // 비용
        if (costText != null)
            costText.text = maxed ? LocalizationManager.Get("upgrade_maxed") : $"${cost}";

        // 버튼
        if (buyButton != null)
        {
            buyButton.interactable = !maxed;
            if (buyButtonText != null)
                buyButtonText.text = maxed
                    ? LocalizationManager.Get("upgrade_maxed")
                    : LocalizationManager.Get("upgrade_buy");
        }

        // 배경 색
        if (cardBackground != null)
        {
            cardBackground.raycastTarget = false;
            cardBackground.color = maxed ? colorMaxLevel : canBuy ? colorAvailable : colorNoCoin;
        }
    }

    void RefreshIcon()
    {
        if (iconImage == null)
            ResolveIconImage();
        if (iconImage == null) return;

        Sprite icon = GetIcon(_type);
        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
    }

    void RefreshLevel(int level)
    {
        if (levelText == null)
            ResolveLevelText();
        if (levelText == null) return;

        levelText.text = $"Lv.{level} / {UpgradeManager.MaxLevel}";
        levelText.raycastTarget = false;
    }

    void ResolveIconImage()
    {
        Transform icon = transform.Find("Image");
        if (icon == null) icon = transform.Find("Icon");
        if (icon != null) iconImage = icon.GetComponent<Image>();
    }

    void ResolveLevelText()
    {
        Transform level = transform.Find("LevelText");
        if (level == null) level = transform.Find("Level");
        if (level != null)
        {
            levelText = level.GetComponent<TMP_Text>();
            return;
        }

        var go = new GameObject("LevelText", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        go.transform.SetSiblingIndex(iconImage != null ? iconImage.transform.GetSiblingIndex() + 1 : transform.childCount - 1);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -96f);
        rect.sizeDelta = new Vector2(-20f, 28f);

        levelText = go.AddComponent<TextMeshProUGUI>();
        levelText.fontSize = 18f;
        levelText.color = Color.white;
        levelText.alignment = TextAlignmentOptions.Center;
        levelText.textWrappingMode = TextWrappingModes.NoWrap;
        levelText.raycastTarget = false;

        TMP_Text template = descText != null ? descText : nameText;
        if (template != null)
        {
            levelText.font = template.font;
            levelText.fontSharedMaterial = template.fontSharedMaterial;
        }
    }

    Sprite GetIcon(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.Speed:
                return speedIcon;
            case UpgradeType.DashSpeed:
                return dashSpeedIcon;
            case UpgradeType.JumpHeight:
                return jumpHeightIcon;
            case UpgradeType.HPup:
                return HpIcon;
            case UpgradeType.Eating:
                return HungerIcon;
            case UpgradeType.DashCooldown:
                return dashCooldownIcon;
            case UpgradeType.TurnCooldown:
                return turnCooldownIcon;
            default:
                return null;
        }
    }

    void OnBuyClicked()
    {
        UpgradeManager.Instance?.Buy(_type);
        Refresh();
    }
}
