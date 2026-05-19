using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed     = 4f;
    [SerializeField] float dashForce     = 12f;
    [SerializeField] float dashDuration  = 0.2f;

    [Header("Jump Feel")]
    [SerializeField] float maxJumpHeight   = 2.5f;
    [SerializeField] float maxJumpApexTime = 0.28f;
    [SerializeField] float minJumpHeight   = 0.4f;
    [SerializeField] float minJumpApexTime = 0.2f;
    [SerializeField] float fallMultiplier  = 3.0f;
    [SerializeField] float preLandDistance = 0.8f;

    [Header("Wall / Ladder Detection")]
    [SerializeField] LayerMask ladderMask;            // Inspector에서 Ladder 레이어 지정

    [Header("🔧 테스트 입력 (개발용)")]
    [SerializeField] float animResetTime = 0.6f;      // Throw·Hurt 자동 리셋 시간

    // ── 컴포넌트 참조 ────────────────────────────────────────────────────────
    Rigidbody2D               _rb;
    PlayerAnimationController _anim;
    Collider2D                _col;
    PlayerInput               _playerInput;
    InputAction               _jumpAction;

    // ── 레이어 마스크 ────────────────────────────────────────────────────────
    int _groundMask;

    // ── 상태 ─────────────────────────────────────────────────────────────────
    float _moveInput;
    bool  _isGrounded;
    bool  _isDucking;
    bool  _isDashing;
    bool  _isOnLadder;
    bool  _isOnWall;
    float _dashTimer;
    float _defaultGravityScale;
    bool  _facingRight = true;
    bool  _jumpHeld;

    // 테스트용 토글 상태
    bool  _testHungry;
    float _throwTimer;
    float _hurtTimer;
    float _fightCooldown;

    // 사망 상태 (게임 오버)
    bool _isDead;

    // Rigidbody2D.GetContacts 재사용 배열 (GC 방지)
    static readonly ContactPoint2D[] _contacts = new ContactPoint2D[8];

    // ── Input System 콜백 ────────────────────────────────────────────────────

    public void OnMove(InputValue value)
    {
        if (_isDucking) return;
        _moveInput = value.Get<float>();
    }

    public void OnJump(InputValue value)
    {
        if (_isDucking) return;
        if (value.isPressed) PressJump();
        else                 ReleaseJump();
    }

    void OnJumpStarted(InputAction.CallbackContext ctx) => PressJump();
    void OnJumpCanceled(InputAction.CallbackContext ctx) => ReleaseJump();

    public void OnDash(InputValue value)
    {
        if (_isDucking) return;
        if (value.isPressed && !_isDashing) StartDash();
    }

    public void OnDuck(InputValue value)
    {
        if (!value.isPressed) return;
        if (_isDucking)
        {
            _isDucking = false;
        }
        else
        {
            if (!_isGrounded) return;
            _isDucking = true;
            _moveInput = 0f;
            _isDashing = false;
        }
    }

    // ── 초기화 ───────────────────────────────────────────────────────────────

    void Awake()
    {
        _rb           = GetComponent<Rigidbody2D>();
        _anim         = GetComponent<PlayerAnimationController>();
        _col          = GetComponent<Collider2D>();
        _playerInput  = GetComponent<PlayerInput>();
        _defaultGravityScale = _rb.gravityScale;
    }

    void OnEnable()
    {
        _jumpAction = _playerInput != null
            ? _playerInput.actions.FindAction("Jump", false) : null;
        if (_jumpAction == null) return;
        _jumpAction.started  += OnJumpStarted;
        _jumpAction.canceled += OnJumpCanceled;
    }

    void OnDisable()
    {
        if (_jumpAction == null) return;
        _jumpAction.started  -= OnJumpStarted;
        _jumpAction.canceled -= OnJumpCanceled;
    }

    void Start()
    {
        _groundMask = LayerMask.GetMask("Ground");
        if (_groundMask == 0)
            _groundMask = ~(1 << gameObject.layer);
    }

    // ── 업데이트 ─────────────────────────────────────────────────────────────

    void Update()
    {
        if (!GameManager.Instance.IsPlaying) return;
        if (_isDead) return;

        // ── 지면 감지 (충돌 법선 기반) ─────────────────────────────────────
        _isGrounded = false;
        int cnt = _rb.GetContacts(_contacts);
        for (int i = 0; i < cnt; i++)
            if (_contacts[i].normal.y > 0.8f) { _isGrounded = true; break; }

        // ── 착지 예측 (Raycast) ────────────────────────────────────────────
        bool aboutToLand = false;
        if (!_isGrounded && _rb.linearVelocity.y < 0f)
        {
            float feetY  = _col != null ? _col.bounds.min.y : transform.position.y - 0.5f;
            var   origin = new Vector2(transform.position.x, feetY);
            var   hit    = Physics2D.Raycast(origin, Vector2.down, 20f, _groundMask);
            if (hit.collider != null && hit.distance < preLandDistance)
                aboutToLand = true;
        }

        // ── 벽 감지 (접촉 법선 기반 — 실제로 닿았을 때만 활성화) ──────────
        _isOnWall = false;
        if (!_isGrounded)
        {
            for (int i = 0; i < cnt; i++)
                if (Mathf.Abs(_contacts[i].normal.x) > 0.8f) { _isOnWall = true; break; }
        }

        // ── 사다리 감지 (OverlapPoint로 Ladder 레이어 확인) ─────────────────
        if (ladderMask != 0)
        {
            var overlapCol = Physics2D.OverlapPoint(transform.position, ladderMask);
            _isOnLadder    = overlapCol != null;
        }

        // ── 대시 타이머 ─────────────────────────────────────────────────────
        if (_isDashing)
        {
            _dashTimer -= Time.deltaTime;
            if (_dashTimer <= 0f) _isDashing = false;
        }

        // ── 테스트 입력 (레거시 Input 사용) ────────────────────────────────
        HandleTestInput();

        // ── 쿨다운 타이머 ──────────────────────────────────────────────────
        if (_throwTimer    > 0f) _throwTimer    -= Time.deltaTime;
        if (_hurtTimer     > 0f) _hurtTimer     -= Time.deltaTime;
        if (_fightCooldown > 0f) _fightCooldown -= Time.deltaTime;

        UpdateFacing();
        _anim?.UpdateState(_moveInput, _isGrounded, aboutToLand,
                           _isDucking, _isDashing, _isOnLadder, _isOnWall);
    }

    /// <summary>
    /// 개발용 테스트 키 입력 — Unity Input System API 사용 (레거시 Input 미사용).
    /// 완성 후 이 메서드 제거 또는 #if UNITY_EDITOR 로 감싸면 됨.
    /// </summary>
    void HandleTestInput()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;

        // ── 항상 가능 (액션 중에도 동작) ───────────────────────────────────

        // G — 배고픈 모션 토글
        if (kb.gKey.wasPressedThisFrame)
        {
            _testHungry = !_testHungry;
            _anim?.SetHungry(_testHungry);
        }

        // Q — 공격 모션 (쿨다운만 체크)
        if (kb.qKey.wasPressedThisFrame && _fightCooldown <= 0f)
        {
            _anim?.TriggerFight();
            _fightCooldown = animResetTime;
        }

        // F — 게임 오버 (이후 모든 입력·이동 차단)
        if (kb.fKey.wasPressedThisFrame)
        {
            _isDead = true;
            _rb.linearVelocity = Vector2.zero;
            _rb.gravityScale   = _defaultGravityScale;
            _anim?.SetDead(true);
        }

        // ── 액션 중에는 아래 입력 전부 차단 ────────────────────────────────
        if (_anim != null && _anim.IsActionPlaying()) return;

        // S — 스틸 모션
        if (kb.sKey.wasPressedThisFrame)
            _anim?.TriggerSteal();

        // H — 피격 모션
        if (kb.hKey.wasPressedThisFrame && _hurtTimer <= 0f)
        {
            _anim?.SetHurt(true);
            _hurtTimer = animResetTime;
        }

        // T — 던지기 모션
        if (kb.tKey.wasPressedThisFrame && _throwTimer <= 0f)
        {
            _anim?.SetThrow(true);
            _throwTimer = animResetTime;
        }

        // E — 음식 먹기 모션
        if (kb.eKey.wasPressedThisFrame)
            _anim?.TriggerEat();

        // R — 잠자기 모션
        if (kb.rKey.wasPressedThisFrame)
            _anim?.TriggerSleep();

        // Shift — Turn/Spin 모션
        if (kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame)
            _anim?.TriggerTurn();
    }

    void FixedUpdate()
    {
        if (!GameManager.Instance.IsPlaying) return;
        if (_isDead) return;
        if (_isDashing) return;

        // 사다리 위에서는 중력 제거 + 수직 이동
        if (_isOnLadder)
        {
            _rb.gravityScale = 0f;
            float vertInput  = Input.GetAxisRaw("Vertical");   // W/S 또는 ↑↓
            _rb.linearVelocity = new Vector2(_moveInput * moveSpeed * 0.5f,
                                             vertInput  * moveSpeed);
            return;
        }

        // 사다리 벗어나면 중력 복원
        if (_rb.gravityScale == 0f)
            _rb.gravityScale = _defaultGravityScale;

        if (_isDucking)
        {
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            return;
        }

        if (_isGrounded && _rb.linearVelocity.y <= 0f)
            _rb.gravityScale = _defaultGravityScale;
        else if (!_isGrounded && _rb.linearVelocity.y < 0f)
            _rb.gravityScale = _defaultGravityScale * fallMultiplier;

        // 벽에 붙어 있으면 낙하 속도 감소 (벽 슬라이드)
        if (_isOnWall && _rb.linearVelocity.y < 0f)
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x,
                                             Mathf.Max(_rb.linearVelocity.y, -2f));
        }

        _rb.linearVelocity = new Vector2(_moveInput * moveSpeed, _rb.linearVelocity.y);
    }

    // ── 점프 ─────────────────────────────────────────────────────────────────

    void Jump()
    {
        float v0 = 2f * maxJumpHeight / maxJumpApexTime;
        _rb.gravityScale   = CalculateJumpGravityScale(v0, maxJumpApexTime);
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, v0);
        _anim?.TriggerJump(true);
    }

    void PressJump()
    {
        if (_isDucking) return;
        if (_jumpHeld)  return;
        _jumpHeld = true;
        if (_isGrounded) Jump();
    }

    void ReleaseJump()
    {
        if (!_jumpHeld) return;
        _jumpHeld = false;
        CutJump();
        _anim?.SetHighJump(false);
    }

    void CutJump()
    {
        if (_rb.linearVelocity.y <= 0f) return;
        float v0min = 2f * minJumpHeight / minJumpApexTime;
        if (_rb.linearVelocity.y > v0min)
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, v0min);
        _rb.gravityScale = CalculateJumpGravityScale(v0min, minJumpApexTime);
    }

    float CalculateJumpGravityScale(float jumpVelocity, float apexTime)
    {
        apexTime = Mathf.Max(0.01f, apexTime);
        return jumpVelocity / (Mathf.Abs(Physics2D.gravity.y) * apexTime);
    }

    // ── 대시 ─────────────────────────────────────────────────────────────────

    void StartDash()
    {
        _isDashing = true;
        _dashTimer = dashDuration;
        float dir  = _facingRight ? 1f : -1f;
        _rb.linearVelocity = new Vector2(dir * dashForce, _rb.linearVelocity.y);
        // isRunning=true → Run 애니메이션 (UpdateState에서 처리)
        // TriggerTurn()은 Shift 키 전용
    }

    // ── 방향 전환 ────────────────────────────────────────────────────────────

    void UpdateFacing()
    {
        if (_moveInput > 0f && !_facingRight) Flip();
        else if (_moveInput < 0f && _facingRight) Flip();
    }

    void Flip()
    {
        _facingRight = !_facingRight;
        Vector3 s = transform.localScale;
        s.x *= -1f;
        transform.localScale = s;
    }

    // ── 외부 참조용 프로퍼티 ─────────────────────────────────────────────────

    public bool IsGrounded => _isGrounded;
    public bool IsDucking  => _isDucking;
    public bool IsOnLadder => _isOnLadder;
    public bool IsOnWall   => _isOnWall;
}
