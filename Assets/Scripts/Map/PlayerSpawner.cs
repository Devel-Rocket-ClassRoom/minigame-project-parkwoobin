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

    [Header("SFX")]
    [SerializeField] private AudioClip sfxBoxLand;

    void PlayBoxLandSfx()
    {
        if (sfxBoxLand == null || AudioManager.Instance == null) return;
        var src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.PlayOneShot(sfxBoxLand, AudioManager.Instance.SfxVolume);
    }

    void Awake()
    {
        // 씬 로드 직후 첫 프레임부터 입력 차단 — 박스 연출이 끝날 때 해제
        GetComponent<PlayerController>()?.SetSpawning(true);
    }

    IEnumerator Start()
    {
        // 세이브 불러오기: 박스가 물리로 낙하하기 전에 비활성화 (FixedUpdate 이전)
        if (GameState.Instance != null && GameState.Instance.hasSavedPosition)
        {
            if (defaultSpawnPoint != null)
                defaultSpawnPoint.gameObject.SetActive(false);
        }

        // 같은 프레임의 모든 Awake/Start가 끝난 뒤 실행 (SpawnPoint 등록 순서 보장)
        yield return null;

        // ── 인트로 컷씬 대기 ─────────────────────────────────────────────────
        if (MapCutsceneManager.Instance != null && !MapCutsceneManager.Instance.IntroComplete)
            yield return new WaitUntil(() => MapCutsceneManager.Instance.IntroComplete);

        // ── 스폰 포인트 결정 ────────────────────────────────────────────────
        string entryID = GameState.Instance != null
            ? GameState.Instance.GetTransitionEntry()
            : null;

        var player = GetComponent<PlayerController>();
        var hunger = FindFirstObjectByType<HungerSystem>();

        // ── 스탯 즉시 복원/초기화 — 박스 연출 전에 HUD가 올바른 값을 표시하도록 ──
        bool restoringFromSave = GameState.Instance != null && GameState.Instance.hasSavedPosition;
        bool restoringFromTransition = !string.IsNullOrEmpty(entryID);

        if ((restoringFromSave || restoringFromTransition) && GameState.Instance != null)
        {
            // HP 복원
            if (GameState.Instance.savedMaxHP > 0)
                player?.SetHp(GameState.Instance.savedHP, GameState.Instance.savedMaxHP);

            if (GameState.Instance.savedMaxHunger > 0f)
                hunger?.SetHunger(GameState.Instance.savedHunger);

            GetCoinKeySystem()?.SetCoinsAndKeys(GameState.Instance.savedCoins,
                                                GameState.Instance.savedKeys);

            // 세이브 파일 불러오기: 저장된 공격력 복원
            if (restoringFromSave)
                player?.SetAttackPower(GameState.Instance.savedAttack);
        }
        else
        {
            // 새 게임: HungerSystem이 DDOL이라 Start()가 재실행되지 않으므로 직접 초기화
            if (hunger != null) hunger.SetHunger(hunger.MaxHunger);
            GetCoinKeySystem()?.SetCoinsAndKeys(10, 0);
        }

        // ── entryID 정리 ─────────────────────────────────────────────────────
        if (GameState.Instance != null)
            GameState.Instance.ClearTransitionEntry();

        // ── 세이브 파일 불러오기: 저장된 위치로 직접 스폰 (연출 없이) ─────────
        if (restoringFromSave && GameState.Instance != null)
        {
            transform.position = new Vector3(
                GameState.Instance.savedPositionX,
                GameState.Instance.savedPositionY,
                transform.position.z);
            GameState.Instance.hasSavedPosition = false;
            player?.SetSpawning(false);
            yield break;
        }

        // ── 스폰 포인트 결정 ─────────────────────────────────────────────────
        playerSpawnPoint spawnPoint = !string.IsNullOrEmpty(entryID)
            ? playerSpawnPoint.Get(entryID)
            : defaultSpawnPoint;

        // entryID·defaultSpawnPoint 모두 없으면 씬에서 첫 번째 SpawnPoint 자동 탐색
        if (spawnPoint == null)
            spawnPoint = FindFirstObjectByType<playerSpawnPoint>();

        if (spawnPoint == null) { player?.SetSpawning(false); yield break; }

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

        // 3) 박스가 착지할 때까지 대기
        var boxRb = spawnPoint.GetComponent<Rigidbody2D>();
        if (boxRb != null)
        {
            spawnPoint.OnLanded += PlayBoxLandSfx;

            yield return new WaitForFixedUpdate();
            float timeout = 5f;
            while (Mathf.Abs(boxRb.linearVelocity.y) > 0.05f && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
            Debug.Log($"[PlayerSpawner] 박스 착지 완료 (timeout={timeout:F2})");

            spawnPoint.OnLanded -= PlayBoxLandSfx;
        }

        // 4) 박스 열기 애니메이션 대기
        Debug.Log("[PlayerSpawner] OpenBox 시작");
        yield return StartCoroutine(spawnPoint.OpenBox());
        Debug.Log("[PlayerSpawner] OpenBox 완료 → SetSpawning(false)");

        // 5) 착지한 박스 위치로 플레이어 이동 → 차단 해제 → 점프 등장
        transform.position = spawnPoint.transform.position;
        player?.SetSpawning(false);
        if (sr != null) sr.enabled = true;
        if (indicator != null) indicator.gameObject.SetActive(true);

        // 한 프레임 대기: UpdateState가 실행돼 _wasGrounded가 올바르게 초기화된 뒤 점프
        yield return null;
        player?.SpawnJump();

        // zone transition으로 도착한 경우 현재 씬·위치로 저장
        if (restoringFromTransition)
            SaveManager.Instance?.AutoSave();
    }

    CoinKeySystem GetCoinKeySystem()
    {
        return CoinKeySystem.Instance != null
            ? CoinKeySystem.Instance
            : FindFirstObjectByType<CoinKeySystem>();
    }
}
