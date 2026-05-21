using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    private float moveSpeed = 4f;
    private float dashForce = 12f;
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
    private int maxHp = 5;
    private float hurtDuration = 0.35f;
    private float invincibleDuration = 1f;

    [Header("Attack HitBox")]
    [SerializeField] GameObject _attackHitBox;
    [SerializeField] float _attackActiveDuration = 0.2f;

    [Header("Action Cooldowns")]
    [SerializeField] float actionResetTime = 0.6f;

    // ── 컴포넌트 참조 ────────────────────────────────────────────────────────
    Rigidbody2D _rb;
    PlayerAnimationController _anim;
    Collider2D _col;
    PlayerInput _playerInput;
    InputAction _jumpAction;

    // ── 레이어 마스크 ────────────────────────────────────────────────────────
    int _groundMask;

    // ── 상태 ─────────────────────────────────────────────────────────────────
    float _rawMoveInput; // 실제 키 상태 (Input System 이벤트가 없어도 보존)
    float _moveInput;    // 유효 이동 입력 (블록 중엔 0)
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
    int _hp;
    bool _isHurt;
    float _hurtTimer;
    float _invincibleTimer;
    Coroutine _attackCoroutine;

    // 액션 쿨다운 / 토글 상태
    bool _isHungry;
    float _throwTimer;
    float _hurtAnimTimer;
    float _fightCooldown;

    // Rigidbody2D.GetContacts 재사용 배열 (GC 방지)
    static readonly ContactPoint2D[] _contacts = new ContactPoint2D[8];

    // ── Input System 콜백 ────────────────────────────────────────────────────

    public void OnMove(InputValue value)
    {
        if (_isDead) return;
        // 실제 키 상태만 보존 — 적용 여부는 Update에서 결정
        _rawMoveInput = value.Get<float>();
    }

    public void OnJump(InputValue value)
    {
        if (_isDead) return;
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
        if (_isDead) return;
        if (_isDucking)
        {
            _isDucking = false;
        }
        else
        {
            if (!_isGrounded) return;
            _isDucking = true;
            _isDashing = false;
        }
    }

    public void OnAttack(InputValue value)
    {
        if (!value.isPressed) return;
        TriggerAttack();
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
        _hp = maxHp;
        if (_attackHitBox == null) _attackHitBox = BuildAttackHitBox();
        _attackHitBox.SetActive(false);
    }

    // Inspector에 연결된 AttackHitBox가 없으면 플레이어 앞에 자동 생성
    GameObject BuildAttackHitBox()
    {
        var go = new GameObject("Attack HitBox");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0.4f, 0f, 0f);
        go.layer = gameObject.layer;
        // Kinematic RB 필수: 자체 RB 없으면 OnTriggerEnter2D가 부모(Player)에만 전달됨
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.simulated = true;
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.5f, 0.8f);
        go.AddComponent<AttackHitBox>();
        return go;
    }

    // ── 업데이트 ─────────────────────────────────────────────────────────────

    void Update()
    {
        if (!GameManager.Instance.IsPlaying) return;
        if (_isDead) return;

        if (_hurtTimer > 0f)
        {
            _hurtTimer -= Time.deltaTime;
            if (_hurtTimer <= 0f) _isHurt = false;
        }
        if (_invincibleTimer > 0f)
            _invincibleTimer -= Time.deltaTime;

        if (_throwTimer > 0f) _throwTimer -= Time.deltaTime;
        if (_hurtAnimTimer > 0f) _hurtAnimTimer -= Time.deltaTime;
        if (_fightCooldown > 0f) _fightCooldown -= Time.deltaTime;

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

        // 매 프레임 rawMoveInput → moveInput 적용 (블록 중엔 0) — fix #9
        bool movementBlocked = _isDucking || (_anim != null && _anim.IsMovementBlocked());
        _moveInput = movementBlocked ? 0f : _rawMoveInput;

        UpdateFacing();
        _anim?.UpdateState(_moveInput, _isGrounded, aboutToLand,
                           _isDucking, _isDashing, _isOnLadder, _isOnWall);
    }

    void FixedUpdate()
    {
        if (!GameManager.Instance.IsPlaying) return;
        if (_isDead) return;
        if (_isHurt) return;
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

    public void Heal(int amount)
    {
        if (_isDead) return;
        _hp = Mathf.Min(maxHp, _hp + amount);
    }

    public void TakeDamage(int amount, float attackerX = 0f)
    {
        if (_isDead || _invincibleTimer > 0f) return;
        _hp = Mathf.Max(0, _hp - amount);
        _invincibleTimer = invincibleDuration;
        Debug.Log($"[Player] HP: {_hp}/{maxHp}");
        _anim?.SetHurt(true);
        if (_hp <= 0) { Die(); return; }
        _isHurt = true;
        _hurtTimer = hurtDuration;
        float dir = transform.position.x >= attackerX ? 1f : -1f;
        transform.position += new Vector3(dir * (3f / 32f), 0f, 0f);
    }

    /// <summary>게임 오버 처리 — 이동·입력을 즉시 차단하고 속도를 0으로 만듦</summary>
    public void Die()
    {
        if (_isDead) return;
        _isDead = true;
        _rb.linearVelocity = Vector2.zero;
        _rb.gravityScale = _defaultGravityScale;
        DisableAttackHitBox();
        _anim?.SetDead(true);
    }

    // ── 액션 트리거 (외부 입력 진입점) ───────────────────────────────────────

    bool CanStartAction()
    {
        if (_isDead) return false;
        if (_anim != null && _anim.IsActionPlaying()) return false;
        return true;
    }

    public void ToggleHungry()
    {
        if (_isDead) return;
        _isHungry = !_isHungry;
        _anim?.SetHungry(_isHungry);
    }

    public void TriggerAttack()
    {
        if (_isDucking) return;
        if (!CanStartAction()) return;
        if (_fightCooldown > 0f) return;
        _anim?.TriggerFight();
        _fightCooldown = actionResetTime;
        EnableAttackHitBox();
    }

    public void TriggerSteal()
    {
        if (!CanStartAction()) return;
        if (!_isGrounded) return;
        _anim?.TriggerSteal();
    }

    public void TriggerHurtAnimation()
    {
        if (_isDead) return;
        if (_hurtAnimTimer > 0f) return;
        _anim?.SetHurt(true);
        _hurtAnimTimer = actionResetTime;
    }

    public void TriggerThrow()
    {
        if (_isDead) return;
        if (_throwTimer > 0f) return;
        _anim?.SetThrow(true);
        _throwTimer = actionResetTime;
    }

    public void TriggerEat()
    {
        if (!CanStartAction()) return;
        _anim?.TriggerEat();
    }

    public void TriggerSleep()
    {
        if (!CanStartAction()) return;
        if (!_isGrounded) return;
        _anim?.TriggerSleep();
    }

    public void TriggerTurn()
    {
        if (!CanStartAction()) return;
        if (!_isGrounded && !IsAscending) return;
        _anim?.TriggerTurn();
    }

    // Animation Event 또는 코드에서 직접 호출 가능
    public void EnableAttackHitBox()
    {
        if (_attackHitBox == null) return;
        if (_attackCoroutine != null) StopCoroutine(_attackCoroutine);
        _attackHitBox.SetActive(true);
        _attackCoroutine = StartCoroutine(AttackActiveRoutine());
    }

    public void DisableAttackHitBox()
    {
        if (_attackCoroutine != null) { StopCoroutine(_attackCoroutine); _attackCoroutine = null; }
        _attackHitBox?.SetActive(false);
    }

    System.Collections.IEnumerator AttackActiveRoutine()
    {
        var col = _attackHitBox.GetComponent<BoxCollider2D>();
        var hitConfig = _attackHitBox.GetComponent<AttackHitBox>();
        int dmg = hitConfig != null ? hitConfig.Damage : 1;
        var hitEnemies = new System.Collections.Generic.HashSet<EnemyBase>();
        float elapsed = 0f;

        while (elapsed < _attackActiveDuration)
        {
            if (col != null)
            {
                // HitBox 위치·크기 기준으로 겹치는 모든 콜라이더 검사
                Vector2 center = col.bounds.center;
                Vector2 size = col.bounds.size;
                var hits = Physics2D.OverlapBoxAll(center, size, 0f);
                foreach (var h in hits)
                {
                    if (h.gameObject == gameObject) continue; // 자기 자신 제외
                    var enemy = h.GetComponentInParent<EnemyBase>();
                    if (enemy != null && hitEnemies.Add(enemy))
                    {
                        Debug.Log($"[PlayerController] 적 적중: {enemy.name}, damage={dmg}");
                        enemy.TakeDamage(dmg, transform.position.x);
                    }
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        _attackHitBox.SetActive(false);
        _attackCoroutine = null;
    }

    // ── 외부 참조용 프로퍼티 ─────────────────────────────────────────────────

    public bool IsGrounded => _isGrounded;
    public bool IsDucking => _isDucking;
    public bool IsOnLadder => _isOnLadder;
    public bool IsOnWall => _isOnWall;
    public bool IsDead => _isDead;
    public int Hp => _hp;
    public int MaxHp => maxHp;
    /// <summary>공중에서 상승 중(점프)이면 true — Turn·Dash 허용 판단에 사용</summary>
    public bool IsAscending => !_isGrounded && _rb != null && _rb.linearVelocity.y > 0f;
}
