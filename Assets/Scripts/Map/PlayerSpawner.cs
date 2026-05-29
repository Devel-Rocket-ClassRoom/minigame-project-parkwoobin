using System.Collections;
using UnityEngine;

/// <summary>
/// 씬 진입 시 스폰 박스 연출 후 플레이어를 등장시키고 GameState 스탯을 복원한다.
/// Player GameObject에 PlayerController와 함께 붙인다.
///
/// [연출 순서]
///   1. 씬 진입 즉시 HP·배고픔·코인·열쇠를 HUD에 반영 (박스 연출 전)
///   2. 플레이어를 스폰 포인트 위치로 이동 + 스프라이트 숨김
///   3. 박스 Collider와 충돌 무시 (영구)
///   4. 박스 열기 애니메이션 재생 → openDuration 대기
///   5. 스프라이트 표시 + SpawnJump (통 하고 튀어오름)
/// </summary>
public class PlayerSpawner : MonoBehaviour
{
    [Tooltip("처음 시작 씬처럼 entryID가 없을 때 사용할 기본 스폰 포인트")]
    [SerializeField] private playerSpawnPoint defaultSpawnPoint;

    IEnumerator Start()
    {
        // 같은 프레임의 모든 Awake/Start가 끝난 뒤 실행 (SpawnPoint 등록 순서 보장)
        yield return null;

        // ── 스폰 포인트 결정 ────────────────────────────────────────────────
        string entryID = GameState.Instance != null
            ? GameState.Instance.GetTransitionEntry()
            : null;

        var player = GetComponent<PlayerController>();
        var hunger = FindFirstObjectByType<HungerSystem>();

        // ── 스탯 즉시 복원/초기화 — 박스 연출 전에 HUD가 올바른 값을 표시하도록 ──
        if (!string.IsNullOrEmpty(entryID) && GameState.Instance != null)
        {
            // 씬 전환: GameState 저장값 복원
            if (GameState.Instance.savedMaxHP > 0)
                player?.SetHp(GameState.Instance.savedHP, GameState.Instance.savedMaxHP);

            if (GameState.Instance.savedMaxHunger > 0f)
                hunger?.SetHunger(GameState.Instance.savedHunger);

            CoinKeySystem.Instance?.SetCoinsAndKeys(GameState.Instance.savedCoins,
                                                   GameState.Instance.savedKeys);
        }
        else
        {
            // 새 게임: HungerSystem이 DDOL이라 Start()가 재실행되지 않으므로 직접 초기화
            if (hunger != null) hunger.SetHunger(hunger.MaxHunger);
        }

        // ── entryID 정리 ─────────────────────────────────────────────────────
        if (GameState.Instance != null)
            GameState.Instance.ClearTransitionEntry();

        // ── 스폰 포인트 없으면 연출 없이 종료 ───────────────────────────────
        playerSpawnPoint spawnPoint = !string.IsNullOrEmpty(entryID)
            ? playerSpawnPoint.Get(entryID)
            : defaultSpawnPoint;

        if (spawnPoint == null) yield break;

        // ── 스폰 연출 ────────────────────────────────────────────────────────

        // 1) 입력·물리 차단 + 스프라이트 숨김 + 인디케이터 숨김
        player?.SetSpawning(true);
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.enabled = false;
        var indicator = GetComponentInChildren<PlayerIndicator>();
        if (indicator != null) indicator.gameObject.SetActive(false);

        // 2) 박스 Collider와 충돌 영구 무시
        var playerCol = GetComponent<Collider2D>();
        var boxCol = spawnPoint.GetComponent<Collider2D>();
        if (playerCol != null && boxCol != null)
            Physics2D.IgnoreCollision(playerCol, boxCol, true);

        // 3) 박스가 착지할 때까지 대기 (Rigidbody2D 속도가 멈출 때까지, 최대 5초)
        var boxRb = spawnPoint.GetComponent<Rigidbody2D>();
        if (boxRb != null)
        {
            float timeout = 5f;
            while (Mathf.Abs(boxRb.linearVelocity.y) > 0.05f && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
        }

        // 4) 박스 열기 애니메이션 대기
        yield return StartCoroutine(spawnPoint.OpenBox());

        // 5) 착지한 박스 위치로 플레이어 이동 → 차단 해제 → 점프 등장
        transform.position = spawnPoint.transform.position;
        player?.SetSpawning(false);
        if (sr != null) sr.enabled = true;
        if (indicator != null) indicator.gameObject.SetActive(true);

        // 한 프레임 대기: UpdateState가 실행돼 _wasGrounded가 올바르게 초기화된 뒤 점프
        yield return null;
        player?.SpawnJump();
    }
}
