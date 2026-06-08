using UnityEngine;

/// <summary>
/// 플레이어가 닿으면 스테이터스를 회복하고 사라지는 아이템.
///
/// [Inspector 설정값 가이드]
/// 생선        hungerDelta=20
/// 치킨(큰것)  hungerDelta=30
/// 치킨(작은것)/피자 hungerDelta=20
/// 도넛/버섯 등 hungerDelta=10
/// 초콜릿      hungerDelta=-10  (독)
/// 별          hpDelta=3, hungerDelta=50
/// 하트        hpDelta=1
/// 코인        coinDelta=1
/// 열쇠        isKey=true
///
/// [씬 설정]
/// - 이 컴포넌트가 붙은 오브젝트에 Trigger Collider2D 하나 추가 (Is Trigger = true)
/// - 물리 바운스가 필요하면 별도의 Non-Trigger Collider2D도 추가
/// - 플레이어 오브젝트는 "Player" 태그 필요
/// </summary>
public class FoodItem : MonoBehaviour
{
    [Header("Status Effects")]
    [SerializeField] float hungerDelta = 10f;   // 배고픔 회복량 (음수 = 독)
    [SerializeField] int hpDelta = 0;     // HP 회복량
    [SerializeField] int coinDelta = 0;     // 코인 획득량
    [SerializeField] bool isKey = false; // 열쇠 아이템 여부

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Apply(other.gameObject);
    }

    void Apply(GameObject playerGO)
    {
        // 코인·열쇠 포함 모든 아이템에 Eat 모션 + 효과음 재생
        playerGO.GetComponent<PlayerController>()?.TriggerEat();

        // HP 회복
        if (hpDelta != 0)
            playerGO.GetComponent<PlayerController>()?.Heal(hpDelta);

        // 배고픔 회복
        if (!Mathf.Approximately(hungerDelta, 0f))
            FindFirstObjectByType<HungerSystem>()?.Eat(hungerDelta);

        // 코인
        if (coinDelta > 0)
            CoinKeySystem.Instance?.AddCoin(coinDelta);

        // 열쇠
        if (isKey)
            CoinKeySystem.Instance?.AddKey();

        Destroy(gameObject);
    }
}
