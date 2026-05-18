using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed    = 4f;
    [SerializeField] float dashForce    = 12f;
    [SerializeField] float dashDuration = 0.2f;

    [Header("Jump Feel")]
    // v₀ = 2 × height / apexTime
    [SerializeField] float maxJumpHeight   = 3.0f;  // 높은 점프 최고 높이 (Unity units)
    [SerializeField] float maxJumpApexTime = 0.35f; // 높은 점프 정점 도달 시간
    [SerializeField] float minJumpHeight   = 0.8f;  // 낮은 점프 최고 높이
    [SerializeField] float minJumpApexTime = 0.18f; // 낮은 점프 정점 도달 시간
    [SerializeField] float fallMultiplier  = 3.5f;  // 하강 중력 배율
    [SerializeField] float preLandDistance = 1.2f;  // 지면과 이 거리 이내일 때 Land 모션 선발동

    Rigidbody2D _rb;
    PlayerAnimationController _anim;
    Collider2D _col;
    PlayerInput _playerInput;
    InputAction _jumpAction;
    int _groundMask;

    float _moveInput;
    bool _isGrounded;
    bool _isDucking;
    bool _isDashing;
    bool _isOnLadder;
    float _dashTimer;
    float _defaultGravityScale;
    bool _facingRight = true;
    bool _jumpHeld;

    // Rigidbody2D.GetContacts 용 재사용 배열 (GC 방지)
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

    void OnJumpStarted(InputAction.CallbackContext context) => PressJump();
    void OnJumpCanceled(InputAction.CallbackContext context) => ReleaseJump();

    public void OnDash(InputValue value)
    {
        if (_isDucking) return;
        if (value.isPressed && !_isDashing)
            StartDash();
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
        _rb   = GetComponent<Rigidbody2D>();
        _anim = GetComponent<PlayerAnimationController>();
        _col   = GetComponent<Collider2D>();
        _playerInput         = GetComponent<PlayerInput>();
        _defaultGravityScale = _rb.gravityScale;
    }

    void OnEnable()
    {
        _jumpAction = _playerInput != null ? _playerInput.actions.FindAction("Jump", false) : null;
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

        // 지면 감지: Rigidbody2D 실제 충돌 법선 기반
        _isGrounded = false;
        int cnt = _rb.GetContacts(_contacts);
        for (int i = 0; i < cnt; i++)
            if (_contacts[i].normal.y > 0.8f) { _isGrounded = true; break; }

        // 착지 예측: 발 아래 Raycast
        bool aboutToLand = false;
        if (!_isGrounded && _rb.linearVelocity.y < 0f)
        {
            float feetY = _col != null ? _col.bounds.min.y : transform.position.y - 0.5f;
            var origin  = new Vector2(transform.position.x, feetY);
            var hit     = Physics2D.Raycast(origin, Vector2.down, 20f, _groundMask);
            if (hit.collider != null && hit.distance < preLandDistance)
                aboutToLand = true;
        }

        if (_isDashing)
        {
            _dashTimer -= Time.deltaTime;
            if (_dashTimer <= 0f) _isDashing = false;
        }

        UpdateFacing();
        _anim?.UpdateState(_moveInput, _isGrounded, aboutToLand, _isDucking, _isDashing, _isOnLadder);
    }

    void FixedUpdate()
    {
        if (!GameManager.Instance.IsPlaying) return;
        if (_isDashing) return;

        if (_isDucking)
        {
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            return;
        }

        if (_isGrounded && _rb.linearVelocity.y <= 0f)
            _rb.gravityScale = _defaultGravityScale;               // 착지 — 중력 복원
        else if (!_isGrounded && _rb.linearVelocity.y < 0f)
            _rb.gravityScale = _defaultGravityScale * fallMultiplier; // 하강 — g_down

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

    // v0 = g × apexTime  →  gravityScale = v0 / (|g| × apexTime)
    float CalculateJumpGravityScale(float jumpVelocity, float apexTime)
    {
        apexTime = Mathf.Max(0.01f, apexTime);
        float gravity = Mathf.Abs(Physics2D.gravity.y);
        return jumpVelocity / (gravity * apexTime);
    }

    // ── 대시 ─────────────────────────────────────────────────────────────────

    void StartDash()
    {
        _isDashing = true;
        _dashTimer = dashDuration;
        float dir  = _facingRight ? 1f : -1f;
        _rb.linearVelocity = new Vector2(dir * dashForce, _rb.linearVelocity.y);
        _anim?.TriggerTurn();
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
}
