using UnityEngine;

public class CatEnemy : EnemyBase
{
    [Header("AI")]
    [SerializeField] float detectionRange = 3f;
    [SerializeField] float attackRange = 0.45f;
    [SerializeField] float attackHeightTolerance = 0.5f;
    [SerializeField] float patrolRange = 3f;
    [SerializeField] float attackCooldown = 3.5f;
    [SerializeField] float jumpForce = 7f;
    [SerializeField] float jumpCooldown = 2f;

    enum State { Idle, Patrol, Chase, Attack, Hit }

    State _state = State.Idle;
    Transform _player;
    float _attackTimer;
    float _idleTimer;
    float _chaseIdleTimer;
    float _runBurstTimer;
    float _nextRunCooldown;
    float _jumpTimer;
    float _runBurstChance;
    float _runCooldownMin, _runCooldownMax;
    float _runDurationMin, _runDurationMax;
    float _enragedTimer;
    float _patrolOriginX;
    int _patrolDir = 1;
    bool _wasGrounded;
    bool _isFalling;

    protected override void Awake()
    {
        (maxHp, moveSpeed, attackPower) = enemyType switch
        {
            EnemyType.Fast   => (2, 2.5f, 1),
            EnemyType.Strong => (6, 1.2f, 2),
            _                => (3, 1.8f, 1),
        };
        base.Awake();
        _patrolOriginX = transform.position.x;
        _idleTimer = Random.Range(1f, 2.5f);

        if (enemyType == EnemyType.Fast)
        {
            _runBurstChance = 0.025f;
            _runCooldownMin = 1f;   _runCooldownMax = 2.5f;
            _runDurationMin = 1.5f; _runDurationMax = 2.5f;
        }
        else
        {
            _runBurstChance = 0.008f;
            _runCooldownMin = 4f;   _runCooldownMax = 8f;
            _runDurationMin = 0.8f; _runDurationMax = 1.5f;
        }
    }

    void Start()
    {
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = enemyType switch
            {
                EnemyType.Fast => new Color(0.55f, 0.35f, 0.1f),  // Fast: 갈색
                EnemyType.Strong => new Color(0.2f, 0.2f, 0.2f),    // Strong: 어두운 회색
                _ => new Color(1f, 0.65f, 0.2f),  // Normal: 주황
            };
    }

    protected override void Update()
    {
        base.Update();
        if (_isDead) return;

        _attackTimer  -= Time.deltaTime;
        _jumpTimer    -= Time.deltaTime;
        _enragedTimer -= Time.deltaTime;

        bool prevGrounded = _wasGrounded;
        _wasGrounded = _isGrounded;
        _isFalling = !_isGrounded && _rb.linearVelocity.y < -0.5f;

        if (!prevGrounded && _isGrounded) _anim?.TriggerLand();

        UpdateAI();

        bool moving   = Mathf.Abs(_rb.linearVelocity.x) > 0.1f;
        bool runBurst = _state == State.Chase && _runBurstTimer > 0f && _chaseIdleTimer <= 0f;
        bool onWall   = _isOnWall && (_state == State.Chase || _state == State.Patrol) && moving;

        if (onWall)
            _anim?.PlayWall();
        else
            _anim?.UpdateState(isMoving: moving, isRunning: runBurst, isFalling: _isFalling);
    }

    bool IsEnraged => _enragedTimer > 0f;

    bool InAttackRange()
    {
        float dx = Mathf.Abs(_player.position.x - transform.position.x);
        float dy = Mathf.Abs(_player.position.y - transform.position.y);
        return dx <= attackRange && dy <= attackHeightTolerance;
    }

    void UpdateAI()
    {
        if (_player == null) return;
        float dist = Mathf.Abs(_player.position.x - transform.position.x);

        // 격노 상태: 감지 범위 확대, 이탈 거리 증가
        float detectThreshold = IsEnraged ? detectionRange : detectionRange * 0.55f;
        float leashDist       = IsEnraged ? detectionRange * 3.5f : detectionRange * 2f;

        switch (_state)
        {
            case State.Idle:
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                _idleTimer -= Time.deltaTime;
                if (_idleTimer <= 0f) _state = State.Patrol;
                if (dist < detectThreshold) _state = State.Chase;
                break;

            case State.Patrol:
                MovePatrol();
                if (dist < detectThreshold) _state = State.Chase;
                break;

            case State.Chase:
                // 격노 중엔 stalking pause 없음
                if (_chaseIdleTimer > 0f && !IsEnraged)
                {
                    _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                    _chaseIdleTimer -= Time.deltaTime;
                    FaceToward(_player.position.x);
                    break;
                }
                _runBurstTimer   -= Time.deltaTime;
                _nextRunCooldown -= Time.deltaTime;
                // 격노 중엔 Run 버스트를 즉시 시작하고 쿨다운 단축
                float burstChance = IsEnraged ? _runBurstChance * 4f : _runBurstChance;
                float cooldownMult = IsEnraged ? 0.3f : 1f;
                if (_runBurstTimer <= 0f && _nextRunCooldown <= 0f && Random.value < burstChance)
                {
                    _runBurstTimer   = Random.Range(_runDurationMin, _runDurationMax) * (IsEnraged ? 1.5f : 1f);
                    _nextRunCooldown = Random.Range(_runCooldownMin, _runCooldownMax) * cooldownMult;
                }
                float chaseSpeed = _runBurstTimer > 0f ? MoveSpeed : MoveSpeed * 0.3f;
                MoveToward(_player.position.x, chaseSpeed);
                TryJump();
                if (InAttackRange()) { _state = State.Attack; break; }
                if (dist > leashDist) { _patrolOriginX = transform.position.x; _state = State.Idle; _idleTimer = Random.Range(1.5f, 3f); break; }
                if (!IsEnraged && Random.value < 0.006f)
                    _chaseIdleTimer = Random.Range(1.2f, 3f);
                break;

            case State.Attack:
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                FaceToward(_player.position.x);
                if (!InAttackRange()) { _state = State.Chase; break; }
                if (_attackTimer <= 0f)
                {
                    _attackTimer = attackCooldown;
                    _anim?.TriggerAttack();
                    if (InAttackRange())
                    {
                        var playerCtrl = _player.GetComponent<PlayerController>();
                        playerCtrl?.TakeDamage(AttackPower, transform.position.x);
                    }
                }
                break;

            case State.Hit:
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                break;
        }
    }

    void TryJump()
    {
        if (!_isGrounded || _jumpTimer > 0f) return;

        float heightDiff = _player.position.y - transform.position.y;
        if (heightDiff > 1.5f) { Jump(); return; }

        float dir = _player.position.x > transform.position.x ? 1f : -1f;
        bool wallAhead = Physics2D.Raycast(transform.position, Vector2.right * dir, 0.7f, LayerMask.GetMask("Ground"));
        if (wallAhead) Jump();
    }

    void Jump()
    {
        _jumpTimer = jumpCooldown;
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
        _anim?.TriggerJump();
    }

    void MovePatrol()
    {
        float targetX = _patrolOriginX + _patrolDir * patrolRange;
        FaceToward(targetX);
        _rb.linearVelocity = new Vector2(_patrolDir * MoveSpeed * 0.6f, _rb.linearVelocity.y);
        if (Mathf.Abs(transform.position.x - targetX) < 0.2f)
        {
            _patrolDir *= -1;
            _state = State.Idle;
            _idleTimer = Random.Range(0.5f, 1.5f);
        }
    }

    void MoveToward(float targetX, float speed)
    {
        float dir = targetX > transform.position.x ? 1f : -1f;
        FaceToward(targetX);
        _rb.linearVelocity = new Vector2(dir * speed, _rb.linearVelocity.y);
    }

    public override void TakeDamage(int amount, float attackerX = 0f)
    {
        _state = State.Hit;
        base.TakeDamage(amount, attackerX);
        if (!_isDead)
        {
            Invoke(nameof(RecoverFromHit), 0.5f);
            // 60% 확률로 격노 — 피격 시 더 공격적으로 추적
            if (Random.value < 0.6f)
            {
                _enragedTimer    = Random.Range(5f, 9f);
                _runBurstTimer   = Random.Range(_runDurationMin, _runDurationMax);
                _nextRunCooldown = 0f;
            }
        }
    }

    void RecoverFromHit()
    {
        if (!_isDead) _state = State.Chase;
    }

}
