using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// PlayerController는 partial로 4개 파일에 분할:
//   PlayerController.cs           — 필드 선언, 라이프사이클, Update/FixedUpdate, 물리 감지
//   PlayerController.Input.cs     — Input System 콜백
//   PlayerController.Movement.cs  — 점프, 대시, 방향 전환
//   PlayerController.Combat.cs    — 데미지, 사망, 공격, 액션 트리거
// 같은 클래스이므로 prefab/Inspector 연결과 외부 호출은 변경 없음.
// ─────────────────────────────────────────────────────────────────────────────

[RequireComponent(typeof(Rigidbody2D))]
public partial class PlayerController : MonoBehaviour
{
    // ── Inspector 노출 필드 ─────────────────────────────────────────────────
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float dashForce = 12f;
    [SerializeField] private float hideSpeedMultiplier = 0.4f;
    private float dashDuration = 0.2f;

    [Header("Jump Feel")]
    private float maxJumpHeight = 2.5f;
    private float maxJumpApexTime = 0.28f;
    private float minJumpHeight = 0.4f;
    private float minJumpApexTime = 0.2f;
    private float fallMultiplier = 3.0f;
    private float preLandDistance = 0.8f;

    [Header("Wall / Ladder Detection")]
    private LayerMask ladderMask;            // Inspector에서 Ladder 레이어 지정

    [Header("Combat")]
    [SerializeField] private int maxHp = 5;
    private float hurtDuration = 0.35f;
    private float invincibleDuration = 1f;

    [Header("Wall Jump")]
    [SerializeField] bool wallJumpEnabled = false;
    [SerializeField] float wallJumpForceX = 5f;   // 벽 반대 방향 킥 강도

    [Header("Attack HitBox")]
    [SerializeField] GameObject _attackHitBox;
    [SerializeField] float _attackActiveDuration = 0.2f;
    [Tooltip("플레이어 공격력. 자식 HitBox에 AttackHitBox 컴포넌트가 있고 Damage>0이면 그 값이 우선")]
    [SerializeField] int attackPower = 1;

    [Header("Action Cooldowns")]
    [SerializeField] float actionResetTime = 0.6f;
    [SerializeField] float turnDuration = 0.5f;  // 턴 모션 중 콜라이더 비활성 시간

    // ── 컴포넌트 참조 ────────────────────────────────────────────────────────
    Rigidbody2D _rb;
    PlayerAnimationController _anim;
    Collider2D _col;

    // ── 레이어 마스크 ────────────────────────────────────────────────────────
    int _groundMask;

    // ── 상태 (입력 / 이동) ───────────────────────────────────────────────────
    float _rawMoveInput; // 실제 키 상태 (Input System 이벤트가 없어도 보존)
    float _moveInput;    // 유효 이동 입력 (블록 중엔 0)
    bool _facingRight = true;
    bool _jumpHeld;
    bool _isDashing;
    float _dashTimer;
    float _defaultGravityScale;

    // ── 상태 (물리 감지) ─────────────────────────────────────────────────────
    bool _isGrounded;
    bool _aboutToLand;     // FixedUpdate에서 갱신 → Update의 애니메이션이 사용
    bool _isOnLadder;
    bool _isOnWall;
    float _wallNormalX;    // 벽 접촉 법선 X (양수=왼쪽 벽, 음수=오른쪽 벽)
    bool _isHiding;   // 조이스틱 아래 → hide 상태 (모바일 전용)

    // ── 상태 (벽 점프) ──────────────────────────────────────────────────────
    float _wallJumpTimer;  // 이 시간 동안 수평 속도를 FixedUpdate가 덮어쓰지 않음

    // ── 상태 (전투) ─────────────────────────────────────────────────────────
    bool _isDead;
    int _hp;
    bool _isHurt;
    float _hurtTimer;
    float _invincibleTimer;
    Coroutine _attackCoroutine;
    Coroutine _turnCoroutine;

    // ── 상태 (액션 쿨다운/토글) ──────────────────────────────────────────────
    bool _isHungry;
    float _throwTimer;
    float _hurtAnimTimer;
    float _fightCooldown;

    // Rigidbody2D.GetContacts 재사용 배열 (GC 방지)
    static readonly ContactPoint2D[] _contacts = new ContactPoint2D[8];

    // ── 라이프사이클 ────────────────────────────────────────────────────────
    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<PlayerAnimationController>();
        _col = GetComponent<Collider2D>();
        _defaultGravityScale = _rb.gravityScale;
    }

    void Start()
    {
        _groundMask = LayerMask.GetMask("Ground");
        if (_groundMask == 0)
            _groundMask = ~(1 << gameObject.layer);
        _hp = maxHp;
        if (_attackHitBox == null) _attackHitBox = BuildAttackHitBox();
        _attackHitBox.SetActive(false);
    }

    // ── 업데이트 ─────────────────────────────────────────────────────────────
    // 규칙:
    //   Update      → 비물리: 타이머, 입력 가공, transform(localScale) 회전, 애니메이션 파라미터
    //   FixedUpdate → 물리: Rigidbody2D 조작, 중력 스케일, 물리 쿼리(GetContacts/Raycast/OverlapPoint)

    void Update()
    {
        if (!GameManager.Instance.IsPlaying) return;
        if (_isDead) return;

        // ── 타이머 감소 (비물리) ──────────────────────────────────────────
        if (_hurtTimer > 0f)
        {
            _hurtTimer -= Time.deltaTime;
            if (_hurtTimer <= 0f) _isHurt = false;
        }
        if (_invincibleTimer > 0f) _invincibleTimer -= Time.deltaTime;
        if (_throwTimer > 0f) _throwTimer -= Time.deltaTime;
        if (_hurtAnimTimer > 0f) _hurtAnimTimer -= Time.deltaTime;
        if (_fightCooldown > 0f) _fightCooldown -= Time.deltaTime;

        if (_isDashing)
        {
            _dashTimer -= Time.deltaTime;
            if (_dashTimer <= 0f) _isDashing = false;
        }
        if (_wallJumpTimer > 0f) _wallJumpTimer -= Time.deltaTime;

        // ── 이동 입력 가공: rawMoveInput → moveInput (블록 중엔 0) ────────
        bool movementBlocked = _anim != null && _anim.IsMovementBlocked();
        _moveInput = movementBlocked ? 0f : _rawMoveInput;

        // ── 방향(스프라이트 플립) + 애니메이션 파라미터 ───────────────────
        UpdateFacing();
        _anim?.UpdateState(_moveInput, _isGrounded, _aboutToLand,
                           _isHiding, _isDashing, _isOnLadder, _isOnWall);
    }

    void FixedUpdate()
    {
        if (!GameManager.Instance.IsPlaying) return;
        if (_isDead) return;

        // ── 1) 물리 상태 갱신: 지면 / 벽 / 착지 예측 / 사다리 ─────────────
        UpdatePhysicsState();

        if (_isHurt) return;
        if (_isDashing) return;           // 대시 중에는 속도를 덮어쓰지 않음
        if (_wallJumpTimer > 0f) return;  // 벽 점프 직후 수평 속도 보존

        // ── 2) 사다리: 중력 제거 + 수직 이동 ─────────────────────────────
        if (_isOnLadder)
        {
            _rb.gravityScale = 0f;
            float vertInput = ReadVerticalInput();           // W/S 또는 ↑↓ (새 Input System)
            _rb.linearVelocity = new Vector2(_moveInput * moveSpeed * 0.5f,
                                             vertInput * moveSpeed);
            return;
        }

        // 사다리에서 벗어났을 때 중력 복원
        if (_rb.gravityScale == 0f)
            _rb.gravityScale = _defaultGravityScale;

        // ── 4) 점프/낙하 중력 스케일 조정 ─────────────────────────────────
        if (_isGrounded && _rb.linearVelocity.y <= 0f)
            _rb.gravityScale = _defaultGravityScale;
        else if (!_isGrounded && _rb.linearVelocity.y < 0f)
            _rb.gravityScale = _defaultGravityScale * fallMultiplier;

        // ── 5) 벽 슬라이드: 낙하 속도 제한 ───────────────────────────────
        if (_isOnWall && _rb.linearVelocity.y < 0f)
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x,
                                             Mathf.Max(_rb.linearVelocity.y, -2f));
        }

        // ── 6) 수평 이동 적용 ────────────────────────────────────────────
        float speedScale = _isHiding ? hideSpeedMultiplier : 1f;
        _rb.linearVelocity = new Vector2(_moveInput * moveSpeed * speedScale, _rb.linearVelocity.y);
    }

    // ── 물리 감지 ────────────────────────────────────────────────────────────
    // 물리 쿼리(GetContacts / Raycast / OverlapPoint)는 모두 FixedUpdate에서 호출
    void UpdatePhysicsState()
    {
        // ── 지면 감지 (충돌 법선 기반) ─────────────────────────────────────
        _isGrounded = false;
        int cnt = _rb.GetContacts(_contacts);
        for (int i = 0; i < cnt; i++)
            if (_contacts[i].normal.y > 0.8f) { _isGrounded = true; break; }

        // ── 착지 예측 (Raycast) ────────────────────────────────────────────
        _aboutToLand = false;
        if (!_isGrounded && _rb.linearVelocity.y < 0f)
        {
            float feetY = _col != null ? _col.bounds.min.y : transform.position.y - 0.5f;
            var origin = new Vector2(transform.position.x, feetY);
            var hit = Physics2D.Raycast(origin, Vector2.down, 20f, _groundMask);
            if (hit.collider != null && hit.distance < preLandDistance)
                _aboutToLand = true;
        }

        // ── 벽 감지 (Enemy 태그 제외 — 적과 닿았을 때 Wall 오작동 방지) ──────
        _isOnWall = false;
        _wallNormalX = 0f;
        if (!_isGrounded)
        {
            for (int i = 0; i < cnt; i++)
            {
                if (_contacts[i].collider.CompareTag("Enemy")) continue;
                if (Mathf.Abs(_contacts[i].normal.x) > 0.8f)
                {
                    _isOnWall = true;
                    _wallNormalX = _contacts[i].normal.x;
                    break;
                }
            }
        }

        // ── 사다리 감지 (OverlapPoint로 Ladder 레이어 확인) ─────────────────
        if (ladderMask != 0)
        {
            var overlapCol = Physics2D.OverlapPoint(transform.position, ladderMask);
            _isOnLadder = overlapCol != null;
        }
    }

    // ── 외부 참조용 프로퍼티 ─────────────────────────────────────────────────
    public bool IsGrounded => _isGrounded;
    public bool IsOnLadder => _isOnLadder;
    public bool IsOnWall => _isOnWall;
    public bool IsDead => _isDead;
    public int Hp => _hp;
    public int MaxHp => maxHp;
    public bool IsHiding => _isHiding;
    /// <summary>공중에서 상승 중(점프)이면 true — Turn·Dash 허용 판단에 사용</summary>
    public bool IsAscending => !_isGrounded && _rb != null && _rb.linearVelocity.y > 0f;
}
