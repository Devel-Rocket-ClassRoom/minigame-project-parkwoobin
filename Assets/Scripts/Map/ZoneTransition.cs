using UnityEngine;

/// <summary>
/// 구역 경계 트리거.
/// 플레이어가 접촉하면 SceneTransitionManager를 통해 씬을 전환한다.
/// 씬 전환 전 HP·배고픔·코인·열쇠를 GameState에 보관해 다음 씬에서 복원한다.
///
/// [인스펙터 설정]
///  - targetScene    : 이동할 씬 이름 (Build Settings에 등록 필요)
///  - targetEntryID  : 대상 씬에 배치된 SpawnPoint의 entryID
///                     (좌표가 아니라 이름표를 넘긴다 → 대상 씬에서 위치 결정)
///
/// [씬 배치]
///  - IsTrigger = true인 Collider2D를 갖는 오브젝트에 붙인다.
///  - 양방향 이동이 필요하면 반대쪽 경계에도 ZoneTransition을 배치한다.
///  - 대상 씬에는 targetEntryID와 같은 ID의 SpawnPoint가 있어야 한다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ZoneTransition : MonoBehaviour
{
    [Header("목적지")]
    [SerializeField] private string targetScene;
    [SerializeField] private string targetEntryID;

    // ── 트리거 ────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogWarning("[ZoneTransition] SceneTransitionManager를 찾을 수 없습니다.");
            return;
        }
        if (SceneTransitionManager.Instance.IsTransitioning) return;

        if (GameState.Instance != null)
        {
            // HP 보관 — 트리거 Collider가 자식 오브젝트에 있을 수 있으므로 부모까지 탐색
            var player = other.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                GameState.Instance.savedHP = player.Hp;
                GameState.Instance.savedMaxHP = player.MaxHp;
                GameState.Instance.savedFacingLeft = player.IsFacingLeft;
            }

            // 배고픔 보관 — 다음 맵 진입 시 최대치로 리셋
            var hunger = FindFirstObjectByType<HungerSystem>();
            if (hunger != null)
            {
                GameState.Instance.savedHunger = hunger.MaxHunger;
                GameState.Instance.savedMaxHunger = hunger.MaxHunger;
            }

            // 코인·열쇠 보관
            if (CoinKeySystem.Instance != null)
            {
                GameState.Instance.savedCoins = CoinKeySystem.Instance.Coins;
                GameState.Instance.savedKeys = CoinKeySystem.Instance.Keys;
            }
        }

        SceneTransitionManager.Instance.TransitionTo(targetScene, targetEntryID);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // 에디터에서 목적지 방향 시각화
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * 0.6f);
    }
#endif
}
