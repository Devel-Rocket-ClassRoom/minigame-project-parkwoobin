using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 4f;
    [SerializeField] float dashForce = 12f;
    [SerializeField] float dashDuration = 0.2f;

    [Header("Jump Feel")]
    [SerializeField] float maxJumpHeight = 2.5f;
    [SerializeField] float maxJumpApexTime = 0.28f;
    [SerializeField] float minJumpHeight = 0.4f;
    [SerializeField] float minJumpApexTime = 0.2f;
    [SerializeField] float fallMultiplier = 3.0f;
    [SerializeField] float preLandDistance = 0.8f;

    [Header("Wall / Ladder Detection")]
    [SerializeField] LayerMask ladderMask;            // Inspector에서 Ladder 레이어 지정

    // ── 컴포넌트 참조 ────────────────────────────────────────────────────────
    Rigidbody2D _rb;
    PlayerAnimationController _anim;
    Collider2D _col;
    PlayerInput _playerInput;
    InputAction _jumpAction;

    // ── 레이어 마스크 ────────────────────────────────────────────────────────
    int _groundMask;

    // ── 상태 ─────────────────────────────────────────────────────────────────
    float _moveInput;
    bool _isGrounded;
    bool _isDucking;
    bool _isDashing;
    bool _isOnLadder;
    bool _isOnWall;
    float _dashTimer;
    float _defaultGravityScale;
    bool _facingRight = true;
    bool _jumpHeld;
    bool _isDead;

    // Rigidbody2D.GetContacts 재사용 배열 (GC 방지)
    static readonly ContactPoint2D[] _contacts = new ContactPoint2D[8];

    // ── Input System 콜백 ────────────────────────────────────────────────────

    public void OnMove(InputValue value)
    {
        if (_isDucking) return;
        if (_isDead) return;
        // Eat·Sleep 중 이동 차단
        if (_anim != null && _anim.IsMovementBlocked()) { _moveInput = 0f; return; }
        _moveInput = value.Get<float>();
    }

    public void OnJump(InputValue value)
    {
        if (_isDucking) return;
        if (value.isPressed) PressJump();
        else ReleaseJump();
    }

    void OnJumpStarted(InputAction.CallbackContext ctx) => PressJump();
    void OnJumpCanceled(InputAction.CallbackContext ctx) => ReleaseJump();

    public void OnDash(InputValue value)
    {
        if (_isDucking) return;
        if (_isDead) return;
        // 액션 재생 중엔 대시 불가 (캔슬 방지) — fix #7
        if (_anim != null && _anim.IsActionPlaying()) return;
        // 바닥 또는 점프(상승) 중에만 가능 — fix #8
        if (!_isGrounded && (_rb == null || _rb.linearVelocity.y <= 0f)) return;
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
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<PlayerAnimationController>();
        _col = GetComponent<Collider2D>();
        _playerInput = GetComponent<PlayerInput>();
        _defaultGravityScale = _rb.gravityScale;
    }

    void OnEnable()
    {
        _jumpAction = _playerInput != null ? _playerInput.actions.FindAction("Jump", false) : null; // PlayerInput이 없거나 Jump 액션이 없으면 null
        if (_jumpAction == null) return;
        _jumpAction.started += OnJumpStarted;
        _jumpAction.canceled += OnJumpCanceled;
    }

    void OnDisable()
    {
        if (_jumpAction == null) return;
        _jumpAction.started -= OnJumpStarted;
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
            float feetY = _col != null ? _col.bounds.min.y : transform.position.y - 0.5f;
            var origin = new Vector2(transform.position.x, feetY);
            var hit = Physics2D.Raycast(origin, Vector2.down, 20f, _groundMask);
            if (hit.collider != null && hit.distance < preLandDistance)
                aboutToLand = true;
        }

        // ── 벽 감지 (Enemy 태그 제외 — 적과 닿았을 때 Wall 오작동 방지) ──────
        _isOnWall = false;
        if (!_isGrounded)
        {
            for (int i = 0; i < cnt; i++)
            {
                if (_contacts[i].collider.CompareTag("Enemy")) continue;
                if (Mathf.Abs(_contacts[i].normal.x) > 0.8f) { _isOnWall = true; break; }
            }
        }

        // ── 사다리 감지 (OverlapPoint로 Ladder 레이어 확인) ─────────────────
        if (ladderMask != 0)
        {
            var overlapCol = Physics2D.OverlapPoint(transform.position, ladderMask);
            _isOnLadder = overlapCol != null;
        }

        // ── 대시 타이머 ─────────────────────────────────────────────────────
        if (_isDashing)
        {
            _dashTimer -= Time.deltaTime;
            if (_dashTimer <= 0f) _isDashing = false;
        }

        // Eat·Sleep 중에는 수평 이동 입력을 매 프레임 차단 — fix #6
        if (_anim != null && _anim.IsMovementBlocked())
            _moveInput = 0f;

        UpdateFacing();
        _anim?.UpdateState(_moveInput, _isGrounded, aboutToLand,
                           _isDucking, _isDashing, _isOnLadder, _isOnWall);
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
            float vertInput = Input.GetAxisRaw("Vertical");   // W/S 또는 ↑↓
            _rb.linearVelocity = new Vector2(_moveInput * moveSpeed * 0.5f,
                                             vertInput * moveSpeed);
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
        _rb.gravityScale = CalculateJumpGravityScale(v0, maxJumpApexTime);
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, v0);
        _anim?.TriggerJump(true);
    }

    void PressJump()
    {
        if (_isDucking) return;
        if (_isDead) return;                                          // fix #4
        if (_anim != null && _anim.IsMovementBlocked()) return;         // fix #6 Eat·Sleep 중 점프 차단
        if (_jumpHeld) return;
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
        float dir = _facingRight ? 1f : -1f;
        _rb.linearVelocity = new Vector2(dir * dashForce, _rb.linearVelocity.y);
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

    // ── 외부 호출 ────────────────────────────────────────────────────────────

    /// <summary>게임 오버 처리 — 이동·입력을 즉시 차단하고 속도를 0으로 만듦</summary>
    public void Die()
    {
        if (_isDead) return;
        _isDead = true;
        _rb.linearVelocity = Vector2.zero;
        _rb.gravityScale = _defaultGravityScale;
    }

    // ── 외부 참조용 프로퍼티 ─────────────────────────────────────────────────

    public bool IsGrounded => _isGrounded;
    public bool IsDucking => _isDucking;
    public bool IsOnLadder => _isOnLadder;
    public bool IsOnWall => _isOnWall;
    public bool IsDead => _isDead;
    /// <summary>공중에서 상승 중(점프)이면 true — Turn·Dash 허용 판단에 사용</summary>
    public bool IsAscending => !_isGrounded && _rb != null && _rb.linearVelocity.y > 0f;
}
