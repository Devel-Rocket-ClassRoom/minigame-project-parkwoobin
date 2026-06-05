using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점 패널. 현재 맵의 3개 업그레이드 카드를 표시한다.
/// UpgradeCardUI 프리팹을 cardContainer 아래에 동적 생성.
/// </summary>
public class ShopPanel : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] GameObject panelRoot;
    [SerializeField] Button     closeButton;
    [SerializeField] Transform  cardContainer;  // 카드 3개를 넣을 부모 (Horizontal Layout Group 권장)

    [Header("카드 프리팹")]
    [Tooltip("UpgradeCardUI 컴포넌트를 가진 프리팹")]
    [SerializeField] GameObject cardPrefab;

    bool _built;

    void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        closeButton?.onClick.AddListener(Hide);
    }

    public void Show()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        Time.timeScale = 0f;
        BuildCards();
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        Time.timeScale = 1f;
    }

    // 스테이지가 바뀌면 카드를 다시 생성
    void BuildCards()
    {
        int stage = GameState.Instance != null ? GameState.Instance.savedStage : 0;

        // 스테이지가 같으면 기존 카드 재사용
        if (_built && cardContainer.childCount == 3) return;

        // 기존 카드 제거
        foreach (Transform child in cardContainer)
            Destroy(child.gameObject);

        _built = false;

        if (UpgradeManager.Instance == null || cardPrefab == null) return;

        var offers = UpgradeManager.Instance.GetOffers();
        foreach (var offer in offers)
        {
            var go   = Instantiate(cardPrefab, cardContainer);
            var card = go.GetComponent<UpgradeCardUI>();
            card?.Setup(offer);
        }

        _built = true;
    }
}
