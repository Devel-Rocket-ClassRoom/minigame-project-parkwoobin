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
    [SerializeField] TMP_Text[] starTexts;   // 5개 ★/☆ TMP
    [SerializeField] Button   buyButton;
    [SerializeField] TMP_Text buyButtonText;
    [SerializeField] Image    cardBackground;

    [Header("색상")]
    [SerializeField] Color colorAvailable  = new Color(0.2f, 0.8f, 0.3f);
    [SerializeField] Color colorMaxLevel   = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] Color colorNoCoin     = new Color(0.8f, 0.3f, 0.2f);

    UpgradeType _type;

    public void Setup(UpgradeType type)
    {
        _type = type;
        Refresh();
        buyButton?.onClick.AddListener(OnBuyClicked);
        UpgradeManager.OnUpgraded += OnUpgraded;
    }

    void OnDestroy() => UpgradeManager.OnUpgraded -= OnUpgraded;

    void OnUpgraded(UpgradeType t, int _)
    {
        if (t == _type) Refresh();
        Refresh(); // 코인 변경으로 다른 카드도 버튼 상태 갱신
    }

    void Refresh()
    {
        if (UpgradeManager.Instance == null) return;
        int level    = UpgradeManager.Instance.GetLevel(_type);
        int cost     = UpgradeManager.Instance.GetCost(_type);
        bool maxed   = level >= UpgradeManager.MaxLevel;
        bool canBuy  = UpgradeManager.Instance.CanBuy(_type);

        // 이름·설명
        if (nameText != null) nameText.text = LocalizationManager.Get($"upgrade_{_type.ToString().ToLower()}_name");
        if (descText != null) descText.text = LocalizationManager.Get($"upgrade_{_type.ToString().ToLower()}_desc");

        // 별 표시
        if (starTexts != null)
            for (int i = 0; i < starTexts.Length; i++)
                if (starTexts[i] != null)
                    starTexts[i].text = i < level ? "★" : "☆";

        // 비용
        if (costText != null)
            costText.text = maxed ? LocalizationManager.Get("upgrade_maxed") : $"{cost}";

        // 버튼
        if (buyButton != null)
        {
            buyButton.interactable = !maxed && canBuy;
            if (buyButtonText != null)
                buyButtonText.text = maxed
                    ? LocalizationManager.Get("upgrade_maxed")
                    : LocalizationManager.Get("upgrade_buy");
        }

        // 배경 색
        if (cardBackground != null)
            cardBackground.color = maxed ? colorMaxLevel : canBuy ? colorAvailable : colorNoCoin;
    }

    void OnBuyClicked()
    {
        UpgradeManager.Instance?.Buy(_type);
    }
}
