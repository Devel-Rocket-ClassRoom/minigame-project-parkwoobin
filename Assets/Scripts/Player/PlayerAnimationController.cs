using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    Animator _anim;
    Rigidbody2D _rb;

    // ── Animator 파라미터 해시 캐싱 (GC 방지) ────────────────────────────────
    // Bool
    static readonly int H_IsWalking = Animator.StringToHash("isWalking");
    static readonly int H_IsRunning = Animator.StringToHash("isRunning");
    static readonly int H_IsDucking = Animator.StringToHash("isDucking");
    static readonly int H_IsFalling = Animator.StringToHash("isFalling");
    static readonly int H_IsHighJump = Animator.StringToHash("isHighJump");
    static readonly int H_IsHungry = Animator.StringToHash("isHungry");
    static readonly int H_IsHurt = Animator.StringToHash("isHurt");
    static readonly int H_IsWall = Animator.StringToHash("isWall");
    static readonly int H_IsLadder = Animator.StringToHash("isLadder");
    static readonly int H_IsThrow = Animator.StringToHash("isThrow");
    static readonly int H_IsDash = Animator.StringToHash("isDash");
    static readonly int H_IsDead = Animator.StringToHash("isDead");

    // State name hashes (CrossFade 직접 전환용)
    static readonly int H_HurtState = Animator.StringToHash("Hurt");
    static readonly int H_ThrowState = Animator.StringToHash("Throw");
    static readonly int H_GameOverState = Animator.StringToHash("GameOver");
    static readonly int H_SleepState = Animator.StringToHash("Sleep");

    // Trigger  ※ Animator에서 Turn 파라미터명이 "Trun"으로 저장돼 있으므로 그대로 맞춤
    static readonly int H_Jump = Animator.StringToHash("Jump");
    static readonly int H_Land = Animator.StringToHash("Land");
    static readonly int H_Turn = Animator.StringToHash("Trun");   // Animator 오타 "Trun" 유지
    static readonly int H_Steal = Animator.StringToHash("Steal");
    static readonly int H_Fight = Animator.StringToHash("Fight");
    static readonly int H_Eat = Animator.StringToHash("Eat");

    // ── 내부 상태 ─────────────────────────────────────────────────────────────
    bool _wasGrounded;
    bool _wasAboutToLand;
    bool _landTriggeredThisFall;

    void Awake()
    {
        _anim = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody2D>();
    }

    // ── 매 프레임 상태 업데이트 (PlayerController.Update 에서 호출) ────────────

    public void UpdateState(float moveInput, bool isGrounded, bool aboutToLand,
                            bool isDucking, bool isDashing, bool isOnLadder, bool isOnWall)
    {
        bool isMoving = Mathf.Abs(moveInput) > 0.01f;

        _anim.SetBool(H_IsWalking, isMoving && !isDashing && isGrounded && !isOnLadder);
        _anim.SetBool(H_IsRunning, isDashing);
        _anim.SetBool(H_IsDucking, isDucking);
        _anim.SetBool(H_IsLadder, isOnLadder);
        _anim.SetBool(H_IsWall, isOnWall);

        // 낙하 감지 (사다리·벽 붙기 중엔 낙하 판정 제외)
        bool isFalling = !isGrounded && !isOnLadder && !isOnWall
                         && _rb != null && _rb.linearVelocity.y < -0.5f;
        _anim.SetBool(H_IsFalling, isFalling);

        // ── Land 모션 발동 ──────────────────────────────────────────────────
        if (!isGrounded && _wasGrounded)
            _landTriggeredThisFall = false;

        bool fallingAboutToLand = aboutToLand && isFalling;

        if (!_landTriggeredThisFall)
        {
            if (fallingAboutToLand && !_wasAboutToLand)
            {
                _anim.SetBool(H_IsFalling, false);
                _anim.SetTrigger(H_Land);
                _landTriggeredThisFall = true;
            }
            else if (isGrounded && !_wasGrounded)
            {
                _anim.SetBool(H_IsFalling, false);
                _anim.SetTrigger(H_Land);
                _landTriggeredThisFall = true;
            }
        }

        _wasAboutToLand = fallingAboutToLand;
        _wasGrounded = isGrounded;
    }

    // ── 외부 호출 — 점프 ─────────────────────────────────────────────────────

    /// <summary>점프 시작 시 호출. isHighJump=true → Cat_jump_2, false → Cat_jump_1</summary>
    public void TriggerJump(bool isHighJump)
    {
        _anim.SetBool(H_IsHighJump, isHighJump);
        _anim.SetTrigger(H_Jump);
    }

    /// <summary>스페이스바 릴리스 시 낮은 점프 포즈로 전환</summary>
    public void SetHighJump(bool value) => _anim.SetBool(H_IsHighJump, value);

    // ── 외부 호출 — 트리거 ───────────────────────────────────────────────────

    /// <summary> Shift 키 → Turn/Spin 모션 (Animator 파라미터명: Trun)</summary>
    public void TriggerTurn() => _anim.SetTrigger(H_Turn);

    /// <summary>S 키 → 스틸 모션</summary>
    public void TriggerSteal() => _anim.SetTrigger(H_Steal);

    /// <summary>Q 키 → 공격 모션</summary>
    public void TriggerFight() => _anim.SetTrigger(H_Fight);

    /// <summary>아이템 먹기 → 먹는 모션</summary>
    public void TriggerEat() => _anim.SetTrigger(H_Eat);

    // ── 외부 호출 — Bool ─────────────────────────────────────────────────────

    /// <summary>게임 오버 — CrossFade로 직접 진입(isDead bool 미사용 → CanTransitionToSelf 루프 차단)</summary>
    public void SetDead(bool value)
    {
        if (value) _anim.CrossFade(H_GameOverState, 0f, 0, 0f);
    }

    /// <summary>R 키 → 잠자기 모션 (애니메이션 끝나면 Idle로 자동 복귀)</summary>
    public void TriggerSleep() => _anim.CrossFade(H_SleepState, 0f, 0, 0f);

    // ── 액션 상태 조회 ────────────────────────────────────────────────────────

    /// <summary>
    /// Sleep·Hurt·Throw·Eat·Steal·Turn·Fight 중 하나가 재생 중이면 true 반환.
    /// Hungry·Attack(Fight)는 항상 가능하므로 호출부에서 별도 처리.
    /// </summary>
    public bool IsActionPlaying()
    {
        var cur = _anim.GetCurrentAnimatorStateInfo(0);
        if (IsActionState(cur)) return true;
        if (_anim.IsInTransition(0))
            return IsActionState(_anim.GetNextAnimatorStateInfo(0));
        return false;
    }

    static bool IsActionState(AnimatorStateInfo info)
        => info.IsName("Sleep") || info.IsName("Hurt") || info.IsName("Throw")
        || info.IsName("Eat") || info.IsName("Steal") || info.IsName("Turn")
        || info.IsName("Fight");

    /// <summary>H 키 · HungerRatio &lt; 0.3f → 배고픈 모션</summary>
    public void SetHungry(bool value) => _anim.SetBool(H_IsHungry, value);

    /// <summary>피격 시 true → CrossFade로 즉각 전환. false는 no-op(ExitTime으로 자동 복귀)</summary>
    public void SetHurt(bool value)
    {
        if (value) _anim.CrossFade(H_HurtState, 0f, 0, 0f);
    }

    /// <summary>T 키 → CrossFade로 직접 진입(isThrow bool 미사용 → CanTransitionToSelf 루프 차단)</summary>
    public void SetThrow(bool value)
    {
        if (value) _anim.CrossFade(H_ThrowState, 0.25f, 0, 0f);
    }

    // TODO: PlayerStats 작성 후 아래 구독 코드 추가
    // void Start()    { PlayerStats.OnHungerChanged += v => SetHungry(v < 0.3f); }
    // void OnDestroy(){ PlayerStats.OnHungerChanged -= v => SetHungry(v < 0.3f); }
}
