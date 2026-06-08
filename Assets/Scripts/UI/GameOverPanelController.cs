using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GameOverPanel + DimOverlay 표시/숨김을 담당한다.
///
/// [Inspector 연결]
///   gameOverPanel  → GameOverPanel 오브젝트
///   dimOverlay     → DimOverlay 오브젝트
///   mainSceneName  → 이동할 메인 씬 이름 (기본값 "Main")
///
/// [버튼 연결]
///   X 버튼    → OnClickClose()
///   메인 버튼 → OnClickMain()
///   종료 버튼 → OnClickQuit()
/// </summary>
public class GameOverPanelController : MonoBehaviour
{
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] GameObject dimOverlay;
    [SerializeField] string mainSceneName = "Main";
    [SerializeField] float extraDelay = 2f;   // 사망 애니메이션 완료 후 추가 대기 시간
    [SerializeField] AudioClip sfxGameOver;

    void Awake()
    {
        // 시작 시 패널·오버레이 숨김
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (dimOverlay != null) dimOverlay.SetActive(false);
    }

    void OnEnable()
    {
        GameManager.OnStateChanged += OnGameStateChanged;
    }

    void OnDisable()
    {
        GameManager.OnStateChanged -= OnGameStateChanged;
    }

    bool _showing;

    void OnGameStateChanged(GameManager.GameState state)
    {
        Debug.Log($"[GameOverPanel] 상태 변경: {state}");
        if (state == GameManager.GameState.GameOver && !_showing)
            StartCoroutine(ShowPanelAfterDelay());
    }

    /// DeadZone 등 고정 딜레이 후 패널을 띄울 때 호출
    public void ShowAfter(float delay)
    {
        if (_showing) return;
        StopAllCoroutines();
        StartCoroutine(ShowAfterCoroutine(delay));
    }

    IEnumerator ShowAfterCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        Show();
    }

    IEnumerator ShowPanelAfterDelay()
    {
        // 플레이어 Animator에서 GameOver(Dead) 상태 재생 완료까지 대기
        // 최대 5초 타임아웃 — 애니메이션이 루프 설정이면 타임아웃 후 진행
        var playerAnim = FindFirstObjectByType<PlayerAnimationController>();
        if (playerAnim != null)
        {
            var animator = playerAnim.GetComponent<Animator>();
            if (animator != null)
            {
                // GameOver 상태에 진입할 때까지 대기 (최대 1초)
                float timeout = 1f;
                while (timeout > 0f && !animator.GetCurrentAnimatorStateInfo(0).IsName("GameOver"))
                {
                    timeout -= Time.deltaTime;
                    yield return null;
                }

                // GameOver 애니메이션 1회 완료 대기 (최대 5초)
                timeout = 5f;
                while (timeout > 0f)
                {
                    var info = animator.GetCurrentAnimatorStateInfo(0);
                    if (info.IsName("GameOver") && info.normalizedTime >= 1f) break;
                    timeout -= Time.deltaTime;
                    yield return null;
                }
            }
        }

        // Dead 애니메이션 종료 후 2초 추가 대기
        yield return new WaitForSeconds(extraDelay);
        Show();
    }

    void Show()
    {
        _showing = true;
        Debug.Log("[GameOverPanel] Show() 호출");
        if (dimOverlay != null) dimOverlay.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        AudioManager.Instance?.PlaySfx(sfxGameOver);
        Time.timeScale = 0f;
    }

    void Hide()
    {
        _showing = false;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (dimOverlay != null) dimOverlay.SetActive(false);
        Time.timeScale = 1f;
    }

    // ── 버튼 콜백 ────────────────────────────────────────────────────────────

    /// X 버튼 — 패널만 닫음
    public void OnClickClose()
    {
        Hide();
    }

    /// 메인 버튼 — 메인 씬으로 이동
    public void OnClickMain()
    {
        Hide();
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.TransitionTo(mainSceneName);
        else
            SceneManager.LoadScene(mainSceneName);
    }

    /// 다시 하기 버튼 — 마지막 체크포인트에서 재시작
    public void OnClickRestart()
    {
        Hide();
        GameManager.Instance?.StartGame();   // GameOver 상태 → Playing으로 리셋

        // 세이브 데이터를 GameState에 복원 → PlayerSpawner가 체크포인트 위치를 사용
        var saveManager = SaveManager.Instance;
        if (saveManager != null)
        {
            var data = saveManager.LoadGame(saveManager.ActiveSlot);
            if (data != null)
            {
                var gs = GameState.Instance;
                if (gs != null)
                {
                    gs.savedPositionX   = data.posX;
                    gs.savedPositionY   = data.posY;
                    gs.hasSavedPosition = true;
                    gs.savedHP          = Mathf.RoundToInt(data.hp);
                    gs.savedMaxHP       = data.maxHp > 0 ? data.maxHp : Mathf.RoundToInt(data.hp);
                    gs.savedHunger      = data.hunger;
                    gs.savedMaxHunger   = 100f;
                    gs.savedCoins       = data.coins;
                    gs.savedKeys        = data.key;
                    gs.savedAttack      = data.attack;
                    gs.savedSkillAttack     = data.skillAttack;
                    gs.savedSkillJump       = data.skillJump;
                    gs.savedSkillDash       = data.skillDash;
                    gs.savedSkillTurn       = data.skillTurn;
                    gs.savedSkillDoubleJump = data.skillDoubleJump;
                    gs.savedSkillWallJump   = data.skillWallJump;
                }
            }
        }

        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.TransitionTo(currentScene);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(currentScene);
    }
}
