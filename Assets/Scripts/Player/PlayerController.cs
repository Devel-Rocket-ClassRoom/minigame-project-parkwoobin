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


    [Header("Jump Feel")]
    private float maxJumpHeight = 2.5f;
    private float maxJumpApexTime = 0.28f;
    private float minJumpHeight = 0.4f;
    private float minJumpApexTime = 0.2f;
    private float fallMultiplier = 3.0f;
    [SerializeField] float maxFallSpeed = 20f;  // 최대 낙하 속도 제한 (터널링 방지)
    private float preLandDistance = 0.8f;

    [Header("Wall / Ladder Detection")]
    private LayerMask ladderMask;            // Inspector에서 Ladder 레이어 지정

    [Header("Combat")]
    [SerializeField] private int maxHp = 20;
    private float hurtDuration = 0.35f;
    private float invincibleDuration = 1f;

    [Header("Wall Jump")]
    [SerializeField] bool wallJumpEnabled = false;
    private float wallJumpForceX = 5f;

    [Header("Double Jump")]
    [SerializeField] bool doubleJumpEnabled = false;

    [Header("Hunger Debuff")]
    [Tooltip("Hunger 0 시 속도·점프에 곱할 배율 (0~1). 기본 0.6 = 40% 감소")]
    [SerializeField] float hungerDebuffMultiplier = 0.6f;
    [Tooltip("이동 중 배고픔 감소 확률을 체크하는 간격")]
    [SerializeField] float moveHungerCheckInterval = 0.2f;

    [Header("Attack HitBox")]
    [SerializeField] GameObject _attackHitBox;
    [SerializeField] float _attackActiveDuration = 0.2f;
    [Tooltip("플레이어 공격력. 자식 HitBox에 AttackHitBox 컴포넌트가 있고 Damage>0이면 그 값이 우선")]
    [SerializeField] int attackPower = 1;

    [Header("Action Cooldowns")]
    [Tooltip("공격·대시·턴 등 행동 후 재사용 가능해질 때까지의 시간")]
    [SerializeField] float actionResetTime = 0.6f;
    [Tooltip("배고픈 상태에서 이동 속도 감소. 모바일에서 조이스틱 아래로 이동할 때 플레이어가 숨는 효과 연출")]
    [SerializeField] float turnDuration = 0.6f;  // 턴 모션 중 콜라이더 비활성 시간

    // ── 컴포넌트 참조 ────────────────────────────────────────────────────────
    Rigidbody2D _rb;
    PlayerAnimationController _anim;
    Collider2D _col;
    HungerSystem _hungerSystem;

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

    // ── 상태 (벽 점프 / 더블 점프) ──────────────────────────────────────────
    float _wallJumpTimer;    // 이 시간 동안 수평 속도를 FixedUpdate가 덮어쓰지 않음
    float _wallJumpDirX;     // 벽 점프 방향 (+1 오른쪽 / -1 왼쪽), 착지 전까지 벽 방향 입력 차단에 사용
    bool _isWallJumping;     // 착지 전까지 true — 벽 방향 입력 차단
    bool _hasDoubleJump;    // 공중에서 한 번 더 점프할 수 있는지 여부
    bool _doubleJumpUsed;   // 더블점프를 사용했으면 true → 착지 전까지 벽점프 차단
    float _wallGripTimer;      // 반대방향키 입력 시 벽 점프 대기 창
    float _savedWallNormalX;   // 대기 창 중 벽 방향 기억
    bool _wallGripUsed;       // 대기 창을 한 번 소모했으면 true → 벽 방향키로 초기화
    bool _dashJustEnded;     // 대시 타이머 만료 직후 한 프레임만 true

    // ── 스폰 연출 차단 ──────────────────────────────────────────────────────
    bool _isSpawning;

    // ── 상태 (전투) ─────────────────────────────────────────────────────────
    bool _isDead;
    int _hp;
    bool _isHurt;
    float _hurtTimer;
    float _invincibleTimer;
    Coroutine _attackCoroutine;
    Coroutine _turnCoroutine;

    // ── Hunger 디버프 기본값 저장 ────────────────────────────────────────────
    float _baseMoveSpeed;
    float _baseDashForce;
    float _baseMaxJumpHeight;

    // ── 상태 (액션 쿨다운/토글) ──────────────────────────────────────────────
    bool _isHungry;
    bool _isStarving;
    float _hurtAnimTimer;
    float _fightCooldown;
    float _moveHungerTimer;

    // Rigidbody2D.GetContacts 재사용 배열 (GC 방지)
    static readonly ContactPoint2D[] _contacts = new ContactPoint2D[8];

    // ── 라이프사이클 ────────────────────────────────────────────────────────
    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<PlayerAnimationController>();
        _col = GetComponent<Collider2D>();
        _defaultGravityScale = _rb.gravityScale;

        // Hunger 디버프 + 업그레이드용 기본값 저장
        _baseMoveSpeed      = moveSpeed;
        _baseDashForce      = dashForce;
        _baseMaxJumpHeight  = maxJumpHeight;
        _baseActionResetTime = actionResetTime;
        _baseTurnDuration   = turnDuration;
    }

    void Start()
    {
        _groundMask = LayerMask.GetMask("Ground");
        if (_groundMask == 0)
            _groundMask = ~(1 << gameObject.layer);
        _hp = maxHp;
        if (_attackHitBox == null) _attackHitBox = BuildAttackHitBox();
        _attackHitBox.SetActive(false);
        _hungerSystem = FindFirstObjectByType<HungerSystem>();
        HungerSystem.OnHungerChanged += OnHungerChanged;
    }

    void OnDestroy()
    {
        HungerSystem.OnHungerChanged -= OnHungerChanged;
    }

    void OnHungerChanged(float current, float max)
    {
        bool hungry = current <= 0f;
        if (_isHungry == hungry) return;
        _isHungry = hungry;
        _anim?.SetHungry(_isHungry);

        // Hunger 0 → 속도·대시·점프 감소 / 회복 → 원래 값 복원
        float mult = hungry ? hungerDebuffMultiplier : 1f;
        moveSpeed     = _baseMoveSpeed     * mult;
        dashForce     = _baseDashForce     * mult;
        maxJumpHeight = _baseMaxJumpHeight * mult;
    }

    // ── 업데이트 ─────────────────────────────────────────────────────────────
    // 규칙:
    //   Update      → 비물리: 타이머, 입력 가공, transform(localScale) 회전, 애니메이션 파라미터
    //   FixedUpdate → 물리: Rigidbody2D 조작, 중력 스케일, 물리 쿼리(GetContacts/Raycast/OverlapPoint)

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
        if (_isDead) return;
        if (_isSpawning) return;

        // ── 타이머 감소 (비물리) ──────────────────────────────────────────
        if (_hurtTimer > 0f)
        {
            _hurtTimer -= Time.deltaTime;
            if (_hurtTimer <= 0f) _isHurt = false;
        }
        if (_invincibleTimer > 0f) _invincibleTimer -= Time.deltaTime;
        if (_hurtAnimTimer > 0f) _hurtAnimTimer -= Time.deltaTime;
        if (_fightCooldown > 0f) _fightCooldown -= Time.deltaTime;

        if (_isDashing)
        {
            _dashTimer -= Time.deltaTime;
            if (_dashTimer <= 0f) { _isDashing = false; _dashJustEnded = true; }
        }
        if (_wallJumpTimer > 0f) _wallJumpTimer -= Time.deltaTime;
        if (_wallGripTimer > 0f)
        {
            _wallGripTimer -= Time.deltaTime;
            if (_wallGripTimer <= 0f) _wallGripUsed = true;  // 만료 → 소모 표시
        }
        if (_isGrounded) { _wallGripTimer = 0f; _wallGripUsed = false; }
        if (_isGrounded) _doubleJumpUsed = false;

        // ── 이동 입력 가공: rawMoveInput → moveInput (블록 중엔 0) ────────
        bool movementBlocked = _anim != null && _anim.IsMovementBlocked();
        _moveInput = movementBlocked ? 0f : _rawMoveInput;

        // ── 방향(스프라이트 플립) + 애니메이션 파라미터 ───────────────────
        // 벽에 붙어있거나 그립 타이머 중에는 벽 방향 유지
        float wallNormalForFacing = _wallGripTimer > 0f ? _savedWallNormalX : _wallNormalX;
        bool pressingAwayNow = _isOnWall && _moveInput != 0f && _wallNormalX * _moveInput > 0f;
        if ((pressingAwayNow || _wallGripTimer > 0f) && wallNormalForFacing != 0f)
        {
            bool wallToRight = wallNormalForFacing < 0f;
            if (wallToRight && !_facingRight) Flip();
            else if (!wallToRight && _facingRight) Flip();
        }
        else UpdateFacing();
        _anim?.UpdateState(_moveInput, _isGrounded, _aboutToLand,
                           _isHiding, _isDashing, _isOnLadder, _isOnWall);
    }

    void FixedUpdate()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
        if (_isDead) return;
        if (_isSpawning) return;

        // ── 1) 물리 상태 갱신: 지면 / 벽 / 착지 예측 / 사다리 ─────────────
        UpdatePhysicsState();



        // ── 공중 대시 종료: 즉시 낙하 시작 ────────────────────────────────────
        if (_dashJustEnded)
        {
            _dashJustEnded = false;
            if (!_isGrounded)
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, -2f);
        }

        if (_isHurt) return;
        if (_isDashing)
        {
            if (!_isGrounded)             // 공중 대시: 중력 제거 → 직선 운동
            {
                _rb.gravityScale = 0f;
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
            }
            return;
        }
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

        // 최대 낙하 속도 제한 — 너무 빠르면 바닥 터널링 발생
        if (_rb.linearVelocity.y < -maxFallSpeed)
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, -maxFallSpeed);

        // ── 5) 벽 고정 ────────────────────────────────────────────────────
        if (_isOnWall && !_isGrounded)
        {
            bool pressingInto = _moveInput != 0f && _wallNormalX * _moveInput < 0f;
            bool pressingAway = _moveInput != 0f && _wallNormalX * _moveInput > 0f;

            if (pressingInto) { _wallGripTimer = 0f; _wallGripUsed = false; }  // 벽 방향키 → 초기화
            if (pressingAway && _wallGripTimer <= 0f && !_wallGripUsed)        // 소모 전에만 시작
            { _wallGripTimer = 0.1f; _savedWallNormalX = _wallNormalX; }

            // 벽 방향키: 중력·수직속도만 고정, 수평은 이동 코드가 벽쪽으로 밀어줌
            if (pressingInto) { _rb.gravityScale = 0f; _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f); }
        }

        // 타이머 중: 벽 방향으로 밀어서 물리 접촉 유지 → _isOnWall = true 복원
        if (_wallGripTimer > 0f && !_isGrounded)
        {
            _rb.gravityScale = 0f;
            _rb.linearVelocity = new Vector2(-_savedWallNormalX * 2f, 0f);
            return;
        }

        // ── 6) 수평 이동 적용 ────────────────────────────────────────────
        // 벽 점프 중 착지 감지: 상승이 끝나고 지면에 닿을 때만 해제
        if (_isWallJumping && _isGrounded && _rb.linearVelocity.y <= 0f)
            _isWallJumping = false;

        float speedScale = _isHiding ? hideSpeedMultiplier : 1f;
        float moveX = _moveInput;
        if (_isWallJumping)
        {
            // 벽 방향으로는 입력 불가, 반대 방향과 중립만 허용
            if (_wallJumpDirX > 0f && moveX < 0f) moveX = 0f;
            else if (_wallJumpDirX < 0f && moveX > 0f) moveX = 0f;
        }
        _rb.linearVelocity = new Vector2(moveX * moveSpeed * speedScale, _rb.linearVelocity.y);
        ConsumeMoveHunger(moveX);
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
        // ── 계단 1타일 제외: 발 위치 기준 1타일 위에 벽이 없으면 계단 → 벽 판정 해제 ─
        if (_isOnWall)
        {
            float feetY   = _col != null ? _col.bounds.min.y : transform.position.y - 0.5f;
            float startX  = _wallNormalX > 0f
                ? (_col != null ? _col.bounds.min.x : transform.position.x)
                : (_col != null ? _col.bounds.max.x : transform.position.x);
            var wDir = new Vector2(-_wallNormalX, 0f);
            bool wallAboveStair = Physics2D.Raycast(
                new Vector2(startX, feetY + 1.1f), wDir, 0.6f, _groundMask).collider != null;
            if (!wallAboveStair)
            {
                _isOnWall = false;
                _wallNormalX = 0f;
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
    public bool IsFacingLeft => !_facingRight;

    /// <summary>UpgradeManager가 업그레이드 보너스를 적용한다.</summary>
    public void SetUpgradeBonuses(float speedBonus, float dashBonus, float jumpBonus,
                                   int hpBonus, float dashCoolReduction, float turnCoolReduction)
    {
        moveSpeed     = (_baseMoveSpeed     + speedBonus) * (_isStarving ? hungerDebuffMultiplier : 1f);
        dashForce     = (_baseDashForce     + dashBonus)  * (_isStarving ? hungerDebuffMultiplier : 1f);
        maxJumpHeight = (_baseMaxJumpHeight + jumpBonus)  * (_isStarving ? hungerDebuffMultiplier : 1f);

        // HP 보너스: 기본 maxHp + 업그레이드 보너스
        int newMaxHp = Mathf.Max(1, maxHp + hpBonus - _upgradeHpBonus);
        _upgradeHpBonus = hpBonus;
        if (newMaxHp != maxHp) SetHp(_hp, newMaxHp);

        // 쿨타임 단축 (최소 0.1초)
        actionResetTime = Mathf.Max(0.1f, _baseActionResetTime - dashCoolReduction);
        turnDuration    = Mathf.Max(0.1f, _baseTurnDuration    - turnCoolReduction);
    }

    // 업그레이드용 기본값 (Awake에서 캡처, SetUpgradeBonuses 이전에 세팅됨)
    float _baseActionResetTime;
    float _baseTurnDuration;
    int   _upgradeHpBonus;

    /// <summary>스폰 연출 중 입력·물리를 차단한다. PlayerSpawner에서 호출.</summary>
    public void SetSpawning(bool spawning)
    {
        _isSpawning = spawning;
        if (_rb == null) return;
        if (spawning)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.gravityScale = 0f;          // 공중에 떠 있지 않도록 중력 차단
        }
        else
        {
            _rb.gravityScale = _defaultGravityScale;
        }
    }
    public bool IsHiding => _isHiding;
    /// <summary>공중에서 상승 중(점프)이면 true — Turn·Dash 허용 판단에 사용</summary>
    public bool IsAscending => !_isGrounded && _rb != null && _rb.linearVelocity.y > 0f;

    public void SetDoubleJump(bool enabled) => doubleJumpEnabled = enabled;
    public void SetWallJump(bool enabled) => wallJumpEnabled = enabled;

    public int AttackPower => attackPower;
    public void SetAttackPower(int value) => attackPower = value;

    void ConsumeHunger(HungerAction action)
    {
        if (_hungerSystem == null)
            _hungerSystem = FindFirstObjectByType<HungerSystem>();
        _hungerSystem?.TryDepleteForAction(action);
    }

    void ConsumeMoveHunger(float moveX)
    {
        if (Mathf.Abs(moveX) <= 0.01f)
        {
            _moveHungerTimer = 0f;
            return;
        }

        _moveHungerTimer += Time.fixedDeltaTime;
        if (_moveHungerTimer < moveHungerCheckInterval) return;

        _moveHungerTimer = 0f;
        ConsumeHunger(HungerAction.Move);
    }
}
