using UnityEngine;

public class CatEnemy : EnemyBase
{
    [Header("Variant")]
    [SerializeField] EnemyType enemyType = EnemyType.Normal;
    public EnemyType Type => enemyType;

    [Header("AI")]
    [SerializeField] float detectionRange = 3f;
    [SerializeField] float patrolRange = 3f;
    [SerializeField] float attackCooldown = 1.5f;

    [Header("Jump / Land")]
    [SerializeField] float jumpForce = 7f;
    [SerializeField] float jumpCooldown = 2f;
    [Tooltip("Chase/Patrol 중 매 프레임 무작위 점프 확률")]
    [SerializeField] float randomJumpChance = 0.014f;
    [Tooltip("Idle 중에도 가끔 통통 튀는 점프 확률 (0이면 사용 안 함)")]
    [SerializeField] float idleJumpChance = 0.005f;
    [Tooltip("이 거리 이내로 지면에 가까워지면 Land 모션을 미리 재생")]
    [SerializeField] float preLandDistance = 0.8f;

    enum State { Idle, Patrol, Chase, Hit }

    State _state = State.Idle;
    Transform _player;
    BoxCollider2D _hitBoxCol;          // HitBox 자식의 BoxCollider2D
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
    bool _landTriggered;       // preLand 예측 트리거 중복 방지
    int  _groundLayerMask;

    protected override void Awake()
    {
        (maxHp, moveSpeed, attackPower) = enemyType switch
        {
            EnemyType.Fast => (2, 2.5f, 1),
            EnemyType.Strong => (6, 1.2f, 2),
            _ => (3, 1.8f, 1),
        };
        base.Awake();

        // HitBox 자식에서 BoxCollider2D 캐싱
        var hitBoxObj = transform.Find("HitBox");
        if (hitBoxObj != null)
            _hitBoxCol = hitBoxObj.GetComponent<BoxCollider2D>();
        Debug.Log($"[CatEnemy] HitBox 캐싱: {(_hitBoxCol != null ? "성공 size=" + _hitBoxCol.size : "실패 — HitBox 자식 또는 BoxCollider2D 없음")}");

        _patrolOriginX = transform.position.x;
        _idleTimer = Random.Range(1f, 2.5f);
        _groundLayerMask = LayerMask.GetMask("Ground");
        if (_groundLayerMask == 0)
            _groundLayerMask = ~(1 << gameObject.layer);

        if (enemyType == EnemyType.Fast)
        {
            _runBurstChance = 0.025f;
            _runCooldownMin = 1f; _runCooldownMax = 2.5f;
            _runDurationMin = 1.5f; _runDurationMax = 2.5f;
        }
        else
        {
            _runBurstChance = 0.008f;
            _runCooldownMin = 4f; _runCooldownMax = 8f;
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
                EnemyType.Fast => new Color(0.55f, 0.35f, 0.1f),
                EnemyType.Strong => new Color(0.2f, 0.2f, 0.2f),
                _ => new Color(1f, 0.65f, 0.2f),
            };
    }

    protected override void Update()
    {
        base.Update();
        if (_isDead) return;

        _attackTimer -= Time.deltaTime;
        _jumpTimer -= Time.deltaTime;
        _enragedTimer -= Time.deltaTime;

        bool prevGrounded = _wasGrounded;
        _wasGrounded = _isGrounded;
        _isFalling = !_isGrounded && _rb.linearVelocity.y < -0.5f;

        // 착지 예측: 0.8 unit 이내로 지면 접근 시 Land 모션을 미리 재생
        CheckPreLand();

        // 실제 착지 순간: 예측이 빠졌을 때 보강
        if (!prevGrounded && _isGrounded)
        {
            if (!_landTriggered) _anim?.TriggerLand();
            _landTriggered = false;   // 다음 점프 대비 리셋
        }

        UpdateAI();

        bool moving = Mathf.Abs(_rb.linearVelocity.x) > 0.1f;
        bool runBurst = _state == State.Chase && _runBurstTimer > 0f && _chaseIdleTimer <= 0f && moving;
        bool onWall = _isOnWall && (_state == State.Chase || _state == State.Patrol) && moving;

        if (onWall) _anim?.PlayWall();
        else _anim?.UpdateState(isMoving: moving, isRunning: runBurst, isFalling: _isFalling);
    }

    bool IsEnraged => _enragedTimer > 0f;

    // 지면에 가까워지면 Land 모션 미리 재생 (한 번만)
    void CheckPreLand()
    {
        if (_isGrounded || _landTriggered) return;
        if (_rb.linearVelocity.y >= 0f) return;   // 상승 중엔 검사 안 함

        float feetY = _col != null ? _col.bounds.min.y : transform.position.y - 0.5f;
        var origin = new Vector2(transform.position.x, feetY);
        var hit = Physics2D.Raycast(origin, Vector2.down, 20f, _groundLayerMask);
        if (hit.collider != null && hit.distance < preLandDistance)
        {
            _anim?.TriggerLand();
            _landTriggered = true;
        }
    }

    void TryRandomJump(float chance)
    {
        if (chance <= 0f) return;
        if (!_isGrounded || _jumpTimer > 0f) return;
        if (Random.value < chance) Jump();
    }

    // HitBox 콜라이더 범위 안에 플레이어가 있는지 확인
    bool PlayerInHitBox()
    {
        if (_hitBoxCol == null || _player == null) return false;

        // 비활성 콜라이더의 bounds는 (0,0,0) → offset/size로 직접 월드 좌표 계산
        float signX = Mathf.Sign(transform.lossyScale.x);
        Vector2 offset = new Vector2(_hitBoxCol.offset.x * signX, _hitBoxCol.offset.y);
        Vector2 center = (Vector2)_hitBoxCol.transform.position + offset;
        Vector2 size = new Vector2(
            _hitBoxCol.size.x * Mathf.Abs(_hitBoxCol.transform.lossyScale.x),
            _hitBoxCol.size.y * Mathf.Abs(_hitBoxCol.transform.lossyScale.y)
        );

        // 레이어 마스크 없이 검색 후 PlayerController 컴포넌트로 판별
        var hits = Physics2D.OverlapBoxAll(center, size, 0f);
        foreach (var h in hits)
        {
            if (h.gameObject == gameObject) continue;
            if (h.GetComponent<PlayerController>() != null ||
                h.GetComponentInParent<PlayerController>() != null)
                return true;
        }
        return false;
    }

    void UpdateAI()
    {
        if (_player == null) return;
        float dist = Mathf.Abs(_player.position.x - transform.position.x);

        float detectThreshold = detectionRange;
        float leashDist = IsEnraged ? detectionRange * 3.5f : detectionRange * 2f;

        switch (_state)
        {
            case State.Idle:
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                _idleTimer -= Time.deltaTime;
                TryRandomJump(idleJumpChance);
                if (_idleTimer <= 0f) _state = State.Patrol;
                if (dist < detectThreshold) _state = State.Chase;
                break;

            case State.Patrol:
                MovePatrol();
                TryRandomJump(randomJumpChance);
                if (dist < detectThreshold) _state = State.Chase;
                break;

            case State.Chase:
                if (_chaseIdleTimer > 0f && !IsEnraged)
                {
                    _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                    _chaseIdleTimer -= Time.deltaTime;
                    FaceToward(_player.position.x);
                    break;
                }

                _runBurstTimer -= Time.deltaTime;
                _nextRunCooldown -= Time.deltaTime;
                float burstChance = IsEnraged ? _runBurstChance * 4f : _runBurstChance;
                float cooldownMult = IsEnraged ? 0.3f : 1f;
                if (_runBurstTimer <= 0f && _nextRunCooldown <= 0f && Random.value < burstChance)
                {
                    _runBurstTimer = Random.Range(_runDurationMin, _runDurationMax) * (IsEnraged ? 1.5f : 1f);
                    _nextRunCooldown = Random.Range(_runCooldownMin, _runCooldownMax) * cooldownMult;
                }

                // HitBox 안에 플레이어가 있으면 멈추고 공격
                if (PlayerInHitBox())
                {
                    _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                    _runBurstTimer = 0f;   // 달리기 애니메이션 즉시 중단
                    FaceToward(_player.position.x);
                    if (_attackTimer <= 0f)
                    {
                        _attackTimer = attackCooldown;
                        _anim?.TriggerAttack();
                        // 데미지는 Animation Event(OnAttackHitFrame)에서 처리
                    }
                    break;
                }

                // HitBox 밖이면 플레이어 쪽으로 이동
                float chaseSpeed = _runBurstTimer > 0f ? MoveSpeed : MoveSpeed * 0.6f;
                MoveToward(_player.position.x, chaseSpeed);
                TryJump();
                TryRandomJump(randomJumpChance);

                if (dist > leashDist)
                {
                    _patrolOriginX = transform.position.x;
                    _state = State.Idle;
                    _idleTimer = Random.Range(1.5f, 3f);
                    break;
                }
                if (!IsEnraged && Random.value < 0.002f)
                    _chaseIdleTimer = Random.Range(0.5f, 1.2f);
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
        bool wallAhead = Physics2D.Raycast(transform.position, Vector2.right * dir, 0.7f, _groundLayerMask);
        if (wallAhead) Jump();
    }

    void Jump()
    {
        Debug.Log($"[CatEnemy:{name}] Jump! grounded={_isGrounded}, vy={_rb.linearVelocity.y:F2} → {jumpForce}");
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
            if (Random.value < 0.6f)
            {
                _enragedTimer = Random.Range(5f, 9f);
                _runBurstTimer = Random.Range(_runDurationMin, _runDurationMax);
                _nextRunCooldown = 0f;
            }
        }
    }

    void RecoverFromHit()
    {
        if (!_isDead) _state = State.Chase;
    }

    // Animation Event: 공격 클립의 마지막 2프레임 시작 시점에서 호출
    public void OnAttackHitFrame()
    {
        if (_isDead || _player == null) return;
        if (!PlayerInHitBox()) return;
        var playerCtrl = _player.GetComponent<PlayerController>();
        playerCtrl?.TakeDamage(attackPower, transform.position.x);
    }
}
