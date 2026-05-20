using UnityEngine;

/// <summary>
/// PlayerController에서 전달받은 상태에 따라 애니메이터 파라미터를 업데이트하고 점프·피격·던지기 등 트리거성 액션을 재생하는 컴포넌트.
/// </summary>


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
    static readonly int H_IsWall = Animator.StringToHash("isWall");
    static readonly int H_IsLadder = Animator.StringToHash("isLadding");  // Animator 오타 "isLadding" 유지

    // State name hashes (CrossFade 직접 전환용)
    static readonly int H_HurtState = Animator.StringToHash("Hurt");
    static readonly int H_ThrowState = Animator.StringToHash("Throw");
    static readonly int H_GameOverState = Animator.StringToHash("GameOver");
    static readonly int H_SleepState = Animator.StringToHash("Sleep");
    static readonly int H_RunState = Animator.StringToHash("Run");

    // Trigger  ※ Animator에서 Turn 파라미터명이 "Trun"으로 저장돼 있으므로 그대로 맞춤
    static readonly int H_Jump = Animator.StringToHash("Jump");
    static readonly int H_Land = Animator.StringToHash("Land");
    static readonly int H_Turn = Animator.StringToHash("Trun");   // Animator 오타 "Trun" 유지
    static readonly int H_Steal = Animator.StringToHash("Steal");
    static readonly int H_Fight = Animator.StringToHash("Fight");
    static readonly int H_Eat = Animator.StringToHash("Eat");

    // ── Eat 상태 이름 해시 (CrossFade 강제 전환용)
    static readonly int H_EatState = Animator.StringToHash("Eat");

    // ── 내부 상태 ─────────────────────────────────────────────────────────────
    bool _wasGrounded;
    bool _wasAboutToLand;
    bool _landTriggeredThisFall;
    bool _eatPending;   // CrossFade 후 Eat 상태 진입 전까지 이동 bool 차단

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

        // Eat 상태 진입 확인 → 진입 완료되면 pending 해제
        if (_eatPending && _anim.GetCurrentAnimatorStateInfo(0).IsName("Eat"))
            _eatPending = false;

        // Eat·Sleep 재생 중이거나 Eat 전환 대기 중엔 이동 bool 차단
        bool blocked = IsMovementBlocked() || _eatPending;

        _anim.SetBool(H_IsWalking, !blocked && isMoving && !isDashing && isGrounded && !isOnLadder);
        _anim.SetBool(H_IsRunning, !blocked && isDashing);
        _anim.SetBool(H_IsDucking, !blocked && isDucking);
        _anim.SetBool(H_IsLadder, isOnLadder);
        _anim.SetBool(H_IsWall, isOnWall);

        // 대시 중: 어느 상태에서든 Run으로 직접 전환 (공중 포함, Eat·Sleep 중 제외) — fix #3
        if (isDashing && !blocked && !_anim.GetCurrentAnimatorStateInfo(0).IsName("Run")
                      && !_anim.IsInTransition(0))
            _anim.Play(H_RunState);

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

    /// 점프 시작 시 호출. isHighJump=true → Cat_jump_2, false → Cat_jump_1
    public void TriggerJump(bool isHighJump)
    {
        _anim.SetBool(H_IsHighJump, isHighJump);
        _anim.SetTrigger(H_Jump);
    }

    /// 점프 버튼 릴리스 시 낮은 점프 포즈로 전환
    public void SetHighJump(bool value) => _anim.SetBool(H_IsHighJump, value);

    // ── 외부 호출 — 트리거 ───────────────────────────────────────────────────

    /// Turn/Spin 모션 (Animator 파라미터명: Trun)
    public void TriggerTurn() => _anim.SetTrigger(H_Turn);

    /// 스틸 모션
    public void TriggerSteal() => _anim.SetTrigger(H_Steal);

    /// 공격 모션
    public void TriggerFight() => _anim.SetTrigger(H_Fight);

    /// 아이템 먹기 모션 — 이동 중에도 강제 전환
    public void TriggerEat()
    {
        _eatPending = true;
        // 이동 bool을 즉시 꺼서 Animator 전환이 방해받지 않게 함
        _anim.SetBool(H_IsWalking, false);
        _anim.SetBool(H_IsRunning, false);
        _anim.CrossFade(H_EatState, 0f, 0, 0f);
    }

    // ── 외부 호출 — Bool ─────────────────────────────────────────────────────

    /// 게임 오버 — CrossFade로 직접 진입(isDead bool 미사용 → CanTransitionToSelf 루프 차단)
    public void SetDead(bool value)
    {
        if (value) _anim.Play(H_GameOverState);
    }

    /// 잠자기 모션 (애니메이션 끝나면 Idle로 자동 복귀)
    public void TriggerSleep() => _anim.Play(H_SleepState);

    /// 배고픈 모션 (HungerRatio &lt; 0.3f 기준은 호출부에서 판단)
    public void SetHungry(bool value) => _anim.SetBool(H_IsHungry, value);

    /// 피격 시 true → 즉시 전환. false는 no-op(ExitTime으로 자동 복귀)
    public void SetHurt(bool value)
    {
        if (value) _anim.Play(H_HurtState);
    }

    /// 던지기 모션 — 즉시 전환(isThrow bool 미사용 → CanTransitionToSelf 루프 차단)
    public void SetThrow(bool value)
    {
        if (value) _anim.Play(H_ThrowState);
    }

    // ── 상태 조회 ─────────────────────────────────────────────────────────────

    /// 
    /// Sleep·Hurt·Throw·Eat·Steal·Turn·Fight 중 하나가 재생 중이면 true.
    /// Hungry·Fight는 항상 가능하므로 호출부에서 별도 처리.
    /// 
    public bool IsActionPlaying()
    {
        var cur = _anim.GetCurrentAnimatorStateInfo(0);
        if (IsActionState(cur)) return true;
        if (_anim.IsInTransition(0))
            return IsActionState(_anim.GetNextAnimatorStateInfo(0));
        return false;
    }

    /// Sleep·Eat 재생 중에는 이동·점프 차단.
    /// Eat/Sleep → 다른 상태로 전환 중일 때는 즉시 해제 (키 홀드 시 끊김 방지)
    public bool IsMovementBlocked()
    {
        if (_anim.IsInTransition(0))
        {
            var nxt = _anim.GetNextAnimatorStateInfo(0);
            // 블로킹 상태로 진입 중 → 차단
            if (nxt.IsName("Sleep") || nxt.IsName("Eat")) return true;
            // 블로킹 상태에서 빠져나가는 중 → 즉시 해제
            return false;
        }
        var cur = _anim.GetCurrentAnimatorStateInfo(0);
        return cur.IsName("Sleep") || cur.IsName("Eat");
    }

    static bool IsActionState(AnimatorStateInfo info)
        => info.IsName("Sleep") || info.IsName("Hurt") || info.IsName("Throw")
        || info.IsName("Eat") || info.IsName("Steal") || info.IsName("Turn")
        || info.IsName("Fight");

    // TODO: PlayerStats 작성 후 아래 구독 코드 추가
    // void Start()    { PlayerStats.OnHungerChanged += v => SetHungry(v < 0.3f); }
    // void OnDestroy(){ PlayerStats.OnHungerChanged -= v => SetHungry(v < 0.3f); }
}
