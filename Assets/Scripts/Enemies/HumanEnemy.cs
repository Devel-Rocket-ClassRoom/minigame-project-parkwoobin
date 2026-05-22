using UnityEngine;

public class HumanEnemy : EnemyBase
{
    public enum HumanVariant { Type1, Type2, Type3 }

    [Header("Variant")]
    [SerializeField] HumanVariant variant = HumanVariant.Type1;

    [Header("Patrol")]
    [SerializeField] float patrolRange = 3f;
    [SerializeField] float patrolWalkSpeed = 1.2f;
    [SerializeField] float idleMin = 0.8f;
    [SerializeField] float idleMax = 2.5f;

    [Header("Detect / Range")]
    [SerializeField] float detectionRange = 6f;
    [SerializeField] float meleeRange = 1.1f;
    [SerializeField] float gunRange = 8f;
    [Tooltip("스폰 위치에서 이만큼 이상 멀어지면 추격을 포기하고 복귀")]
    [SerializeField] float leashRange = 5f;

    [Header("Chase: walk/run 전환")]
    [SerializeField] float walkSpeedRatio = 0.55f;   // run 대비 walk 속도 비율
    [SerializeField] float runBurstMin = 1.2f;
    [SerializeField] float runBurstMax = 2.5f;
    [SerializeField] float walkPhaseMin = 0.8f;
    [SerializeField] float walkPhaseMax = 1.6f;

    [Header("Attack Timing")]
    [SerializeField] float meleeCooldown = 1.3f;
    [SerializeField] float gunCooldown = 2.5f;
    [SerializeField] float aimDuration = 0.7f;        // 빨간 선이 늘어나는 시간
    [SerializeField] float aimHoldDuration = 0.1f;    // 다 늘어난 뒤 잠깐 고정
    [SerializeField] float meleeStateDuration = 0.6f;
    [SerializeField] float hitStateDuration = 0.4f;

    [Header("Bullet")]
    [SerializeField] Bullet bulletPrefab;
    [SerializeField] float bulletSpawnXOffset = 0.7f;
    [SerializeField] float bulletSpawnYOffset = 0.1f;

    [Header("Jump")]
    [SerializeField] float jumpForce = 6f;
    [SerializeField] float jumpCooldown = 2.5f;
    [Tooltip("Patrol/Chase 중 매 프레임 점프할 확률")]
    [SerializeField] float randomJumpChance = 0.012f;
    [Tooltip("Idle 상태 중에도 가끔 점프할 확률(체감용 — 0이면 사용 안 함)")]
    [SerializeField] float idleJumpChance = 0.004f;

    enum State { Idle, Patrol, Chase, MeleeAttack, Aim, Hit }

    HumanAnimationController _humanAnim;
    Transform _player;
    PlayerController _playerCtrl;
    LineRenderer _aimLine;
    State _state = State.Idle;

    float _idleTimer;
    float _attackTimer;
    float _patrolOriginX;
    int _patrolDir = 1;
    float _jumpTimer;

    // walk/run 전환
    float _runBurstTimer;   // > 0 이면 달리는 중
    float _walkPhaseTimer;  // > 0 이면 걷는 중
    bool _isRunningNow;

    // Aim 진행도
    Coroutine _aimRoutine;

    bool HasGun => variant == HumanVariant.Type1 || variant == HumanVariant.Type3;

    protected override void Awake()
    {
        // Variant 기본 스탯 (Inspector 값보다 우선)
        switch (variant)
        {
            case HumanVariant.Type1: maxHp = 4; moveSpeed = 2.2f; attackPower = 1; break;
            case HumanVariant.Type2: maxHp = 6; moveSpeed = 2.0f; attackPower = 2; break;
            case HumanVariant.Type3: maxHp = 3; moveSpeed = 2.7f; attackPower = 1; break;
        }
        base.Awake();

        _humanAnim = GetComponent<HumanAnimationController>();
        _patrolOriginX = transform.position.x;
        _idleTimer = Random.Range(idleMin, idleMax);

        SetupAimLine();
    }

    void Start()
    {
        var p = GameObject.FindWithTag("Player");
        if (p != null)
        {
            _player = p.transform;
            _playerCtrl = p.GetComponent<PlayerController>();
        }
    }

    void SetupAimLine()
    {
        if (!HasGun) return;
        var go = new GameObject("AimLine");
        go.transform.SetParent(transform, false);
        _aimLine = go.AddComponent<LineRenderer>();
        _aimLine.positionCount = 2;
        _aimLine.useWorldSpace = true;
        _aimLine.startWidth = 0.05f;
        _aimLine.endWidth = 0.05f;
        var shader = Shader.Find("Sprites/Default");
        if (shader != null) _aimLine.material = new Material(shader);
        _aimLine.startColor = new Color(1f, 0.1f, 0.1f, 0.9f);
        _aimLine.endColor   = new Color(1f, 0.1f, 0.1f, 0.0f);  // 끝으로 갈수록 페이드
        _aimLine.sortingOrder = 10;
        _aimLine.enabled = false;
    }

    protected override void Update()
    {
        base.Update();
        if (_isDead) return;

        _attackTimer -= Time.deltaTime;
        _jumpTimer   -= Time.deltaTime;

        UpdateAI();

        bool moving  = Mathf.Abs(_rb.linearVelocity.x) > 0.1f;
        bool running = _isRunningNow && moving && (_state == State.Chase);
        bool falling = !_isGrounded && _rb.linearVelocity.y < -0.5f;
        _humanAnim?.UpdateState(isMoving: moving, isRunning: running, isFalling: falling);
    }

    void UpdateAI()
    {
        if (_player == null) return;
        float dx = _player.position.x - transform.position.x;
        float dist = Mathf.Abs(dx);
        float dy = _player.position.y - transform.position.y;

        // 스폰 위치 기반 활동 반경 — "적이 자기 활동 범위 안에 있는지"가 아니라
        // "플레이어가 적의 활동 범위 안에 들어왔는지"가 추격 시작/유지 조건
        float playerDistFromOrigin = Mathf.Abs(_player.position.x - _patrolOriginX);
        bool playerInLeash = playerDistFromOrigin < leashRange;

        switch (_state)
        {
            case State.Idle:
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                _isRunningNow = false;
                _idleTimer -= Time.deltaTime;
                // Idle 중에도 가끔 점프 (제자리에서 통통)
                if (idleJumpChance > 0f && Random.value < idleJumpChance) TryRandomJump();
                if (_idleTimer <= 0f) _state = State.Patrol;
                // 플레이어가 활동 범위 안 + 감지 거리 안일 때만 Chase
                if (dist < detectionRange && playerInLeash) _state = State.Chase;
                break;

            case State.Patrol:
                MovePatrol();
                TryRandomJump();
                if (dist < detectionRange && playerInLeash) _state = State.Chase;
                break;

            case State.Chase:
                FaceToward(_player.position.x);

                // ── 플레이어가 활동 범위를 벗어나면 추격 포기 ──────────────
                if (!playerInLeash)
                {
                    _state = State.Patrol;
                    _isRunningNow = false;
                    break;
                }

                // 근접 공격 가능 (총보다 우선)
                if (dist <= meleeRange && Mathf.Abs(dy) < 1.2f)
                {
                    _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                    _isRunningNow = false;
                    if (_attackTimer <= 0f) StartMelee();
                    break;
                }
                // 총 사거리
                if (HasGun && dist <= gunRange && Mathf.Abs(dy) < 2.0f)
                {
                    _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                    _isRunningNow = false;
                    if (_attackTimer <= 0f) StartAim();
                    break;
                }

                // ── walk/run 전환 ──────────────────────────────────────────
                UpdateRunWalkPhase();
                float chaseSpeed = _isRunningNow ? MoveSpeed : MoveSpeed * walkSpeedRatio;
                float dir = dx > 0f ? 1f : -1f;
                _rb.linearVelocity = new Vector2(dir * chaseSpeed, _rb.linearVelocity.y);

                TryJump(dx, dy);
                TryRandomJump();

                // 플레이어 감지 범위 밖이면 복귀
                if (dist > detectionRange * 1.8f)
                {
                    _state = State.Patrol;
                    _isRunningNow = false;
                }
                break;

            case State.MeleeAttack:
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                break;

            case State.Aim:
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                // 조준 중에도 거리 체크 — 근접 진입 시 즉시 캔슬
                if (dist <= meleeRange && Mathf.Abs(dy) < 1.2f)
                {
                    CancelAim();
                    _state = State.Chase;          // 다음 프레임에서 근접 처리
                    _attackTimer = 0f;             // 즉시 공격 가능
                    break;
                }
                // 조준 중 플레이어가 사거리 밖으로 도망간 경우도 캔슬
                if (HasGun && dist > gunRange * 1.2f)
                {
                    CancelAim();
                    _state = State.Chase;
                    break;
                }
                break;

            case State.Hit:
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                break;
        }
    }

    void CancelAim()
    {
        if (_aimRoutine != null) { StopCoroutine(_aimRoutine); _aimRoutine = null; }
        CleanupAimLine();
    }

    // ── Patrol ───────────────────────────────────────────────────────────────
    void MovePatrol()
    {
        float targetX = _patrolOriginX + _patrolDir * patrolRange;
        FaceToward(targetX);
        _rb.linearVelocity = new Vector2(_patrolDir * patrolWalkSpeed, _rb.linearVelocity.y);
        _isRunningNow = false;
        if (Mathf.Abs(transform.position.x - targetX) < 0.2f)
        {
            _patrolDir *= -1;
            _state = State.Idle;
            _idleTimer = Random.Range(idleMin, idleMax);
        }
    }

    // ── walk/run 전환 로직 ──────────────────────────────────────────────────
    void UpdateRunWalkPhase()
    {
        if (_runBurstTimer > 0f)
        {
            _runBurstTimer -= Time.deltaTime;
            _isRunningNow = true;
            if (_runBurstTimer <= 0f)
                _walkPhaseTimer = Random.Range(walkPhaseMin, walkPhaseMax);
        }
        else if (_walkPhaseTimer > 0f)
        {
            _walkPhaseTimer -= Time.deltaTime;
            _isRunningNow = false;
            if (_walkPhaseTimer <= 0f)
                _runBurstTimer = Random.Range(runBurstMin, runBurstMax);
        }
        else
        {
            // 초기: 달리기 먼저
            _runBurstTimer = Random.Range(runBurstMin, runBurstMax);
            _isRunningNow = true;
        }
    }

    // ── 근접 공격 ───────────────────────────────────────────────────────────
    void StartMelee()
    {
        _attackTimer = meleeCooldown;
        _state = State.MeleeAttack;
        int meleeVariantUsed = (variant == HumanVariant.Type2) ? Random.Range(0, 3) : 0;
        _humanAnim?.TriggerFight(meleeVariantUsed);
        Invoke(nameof(EndMeleeState), meleeStateDuration);
    }

    void EndMeleeState()
    {
        if (_state == State.MeleeAttack) _state = State.Chase;
    }

    /// <summary>Animation Event: 근접 공격 클립의 마지막 2프레임 시점에서 호출</summary>
    public void OnAttackHitFrame()
    {
        if (_isDead || _player == null) return;
        float d = Mathf.Abs(_player.position.x - transform.position.x);
        if (d <= meleeRange + 0.3f && Mathf.Abs(_player.position.y - transform.position.y) < 1.4f)
            _playerCtrl?.TakeDamage(attackPower, transform.position.x);
    }

    // ── 원거리(총) ──────────────────────────────────────────────────────────
    void StartAim()
    {
        _attackTimer = gunCooldown;
        _state = State.Aim;
        _aimTargetPos = _player.position;
        _humanAnim?.TriggerShot();

        if (_aimRoutine != null) StopCoroutine(_aimRoutine);
        _aimRoutine = StartCoroutine(AimRoutine());
    }

    Vector3 _aimTargetPos;

    System.Collections.IEnumerator AimRoutine()
    {
        if (_aimLine != null) _aimLine.enabled = true;

        // 1) 선이 총구→타겟까지 점진적으로 늘어남
        float t = 0f;
        while (t < aimDuration)
        {
            if (_isDead || _state != State.Aim) { CleanupAimLine(); yield break; }
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / aimDuration);
            if (_aimLine != null)
            {
                Vector3 origin = BulletOrigin();
                Vector3 end = Vector3.Lerp(origin, _aimTargetPos, p);
                _aimLine.SetPosition(0, origin);
                _aimLine.SetPosition(1, end);
            }
            yield return null;
        }

        // 2) 완전히 닿은 상태로 살짝 고정
        float hold = 0f;
        while (hold < aimHoldDuration)
        {
            if (_isDead || _state != State.Aim) { CleanupAimLine(); yield break; }
            hold += Time.deltaTime;
            if (_aimLine != null)
            {
                Vector3 origin = BulletOrigin();
                _aimLine.SetPosition(0, origin);
                _aimLine.SetPosition(1, _aimTargetPos);
            }
            yield return null;
        }

        // 3) 선 사라지면서 총알 발사
        CleanupAimLine();
        FireBullet();
    }

    void CleanupAimLine()
    {
        if (_aimLine != null) _aimLine.enabled = false;
    }

    void FireBullet()
    {
        if (bulletPrefab != null && _player != null)
        {
            Vector3 origin = BulletOrigin();
            var b = Instantiate(bulletPrefab, origin, Quaternion.identity);
            Vector2 dir = ((Vector2)(_aimTargetPos - origin));
            if (dir.sqrMagnitude < 0.0001f) dir = _facingRight ? Vector2.right : Vector2.left;
            b.Launch(dir.normalized, gameObject, attackPower);
        }
        else
        {
            Debug.LogWarning($"[HumanEnemy] {name}: bulletPrefab 미설정");
        }
        _state = State.Chase;
    }

    Vector3 BulletOrigin()
    {
        float xOff = _facingRight ? bulletSpawnXOffset : -bulletSpawnXOffset;
        return transform.position + new Vector3(xOff, bulletSpawnYOffset, 0f);
    }

    // ── 점프 ─────────────────────────────────────────────────────────────────
    void TryJump(float dx, float dy)
    {
        if (!_isGrounded || _jumpTimer > 0f) return;
        float dir = dx > 0f ? 1f : -1f;
        bool wallAhead = Physics2D.Raycast(transform.position, Vector2.right * dir, 0.7f, _groundMask);
        if (wallAhead || dy > 1.2f)
            DoJump();
    }

    void TryRandomJump()
    {
        if (!_isGrounded || _jumpTimer > 0f) return;
        if (Random.value < randomJumpChance) DoJump();
    }

    void DoJump()
    {
        Debug.Log($"[HumanEnemy:{name}] Jump! grounded={_isGrounded}, vy={_rb.linearVelocity.y:F2} → {jumpForce}");
        _jumpTimer = jumpCooldown;
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
        _humanAnim?.TriggerJump();
    }

    // ── 피격 / 사망 ─────────────────────────────────────────────────────────
    public override void TakeDamage(int amount, float attackerX = 0f)
    {
        if (_isDead) return;
        CancelAim();   // 조준 중이었으면 캔슬

        _state = State.Hit;
        _humanAnim?.PlayHurt();
        base.TakeDamage(amount, attackerX);
        if (!_isDead) Invoke(nameof(RecoverFromHit), hitStateDuration);
    }

    void RecoverFromHit()
    {
        if (!_isDead && _state == State.Hit) _state = State.Chase;
    }

    protected override void Die()
    {
        CancelAim();
        _humanAnim?.PlayDead();
        base.Die();
    }
}
