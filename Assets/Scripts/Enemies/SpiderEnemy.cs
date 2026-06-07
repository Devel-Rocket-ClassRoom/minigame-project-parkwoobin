using UnityEngine;

/// <summary>
/// 거미 적. 감지 범위 내 플레이어에게 침 공격(BulletSpider)만 사용.
///
/// [Animator 파라미터]
///   isWalking (bool), Shoot (trigger), Hit (trigger), Dead (trigger)
/// </summary>
public class SpiderEnemy : EnemyBase
{
    [Header("Patrol")]
    [SerializeField] float patrolRange = 3f;
    [SerializeField] float patrolSpeed = 1f;
    [SerializeField] float idleMin     = 1f;
    [SerializeField] float idleMax     = 2.5f;

    [Header("Detect / Range")]
    [SerializeField] float detectionRange = 6f;
    [SerializeField] float shootRange     = 5f;
    [SerializeField] float leashRange     = 7f;

    [Header("Attack Timing")]
    [SerializeField] float shootCooldown      = 3f;
    [SerializeField] float shootStateDuration = 0.8f;
    [SerializeField] float hitStateDuration   = 0.35f;
    [SerializeField] float postActionStun     = 0.4f;
    [Tooltip("침 모션 시작 후 HitBox 활성화까지 딜레이(초)")]
    [SerializeField] float shootHitDelay      = 0.35f;

    [Header("SFX")]
    [SerializeField] AudioClip sfxShoot;

    // ── 상태 ─────────────────────────────────────────────────────────────────
    enum State { Idle, Patrol, Chase, Shoot, Hit }
    State _state = State.Idle;

    SpiderAnimationController _spiderAnim;
    Transform        _player;
    GameObject       _spiderBulletVisual;

    float _idleTimer;
    float _attackTimer;
    float _stunTimer;
    float _patrolOriginX;
    int   _patrolDir = 1;

    // ── 초기화 ───────────────────────────────────────────────────────────────
    protected override void Awake()
    {
        base.Awake();
        _spiderAnim    = GetComponent<SpiderAnimationController>();
        _patrolOriginX = transform.position.x;
        _idleTimer     = Random.Range(idleMin, idleMax);
        _facingRight   = false;

        _spiderBulletVisual = transform.Find("SpiderBullet")?.gameObject;
        if (_spiderBulletVisual != null)
            _spiderBulletVisual.SetActive(false);
    }

    void Start()
    {
        var p = GameObject.FindWithTag("Player");
        if (p != null) _player = p.transform;
    }

    protected override void OnRespawn()
    {
        _state       = State.Idle;
        _idleTimer   = Random.Range(idleMin, idleMax);
        _attackTimer = 0f;
        _stunTimer   = 0f;
        if (_spiderBulletVisual != null) _spiderBulletVisual.SetActive(false);
    }

    // ── 메인 루프 ────────────────────────────────────────────────────────────
    protected override void Update()
    {
        base.Update();
        if (_isDead) return;

        _attackTimer -= Time.deltaTime;
        _stunTimer   -= Time.deltaTime;

        UpdateAI();

        bool walking = _state == State.Patrol || (_state == State.Chase && Mathf.Abs(_rb.linearVelocity.x) > 0.1f);
        _spiderAnim?.SetWalking(walking);
    }

    void UpdateAI()
    {
        if (_player == null) return;

        float dx   = _player.position.x - transform.position.x;
        float dist = Mathf.Abs(dx);
        float dy   = Mathf.Abs(_player.position.y - transform.position.y);
        bool playerInLeash = Mathf.Abs(_player.position.x - _patrolOriginX) < leashRange;

        switch (_state)
        {
            case State.Idle:
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                _idleTimer -= Time.deltaTime;
                if (_idleTimer <= 0f) _state = State.Patrol;
                if (dist < detectionRange && playerInLeash) _state = State.Chase;
                break;

            case State.Patrol:
                MovePatrol();
                if (dist < detectionRange && playerInLeash) _state = State.Chase;
                break;

            case State.Chase:
                FaceToward(_player.position.x);

                if (!playerInLeash) { _state = State.Patrol; break; }
                if (_stunTimer > 0f) { _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y); break; }

                if (dist <= shootRange && dy < 2f)
                {
                    _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                    if (_attackTimer <= 0f) StartShoot();
                    break;
                }

                if (!IsValidGroundAhead(dx > 0f ? 1f : -1f))
                {
                    _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                    break;
                }
                _rb.linearVelocity = new Vector2((dx > 0f ? 1f : -1f) * moveSpeed, _rb.linearVelocity.y);

                if (dist > detectionRange * 1.8f) _state = State.Patrol;
                break;

            case State.Shoot:
            case State.Hit:
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                break;
        }
    }

    // ── 순찰 ─────────────────────────────────────────────────────────────────
    void MovePatrol()
    {
        float targetX = _patrolOriginX + _patrolDir * patrolRange;
        FaceToward(targetX);

        if (!IsValidGroundAhead(_patrolDir) || _isOnWall)
        {
            _patrolDir *= -1;
            _state     = State.Idle;
            _idleTimer = Random.Range(idleMin, idleMax);
            return;
        }

        _rb.linearVelocity = new Vector2(_patrolDir * patrolSpeed, _rb.linearVelocity.y);

        if (Mathf.Abs(transform.position.x - targetX) < 0.2f)
        {
            _patrolDir *= -1;
            _state     = State.Idle;
            _idleTimer = Random.Range(idleMin, idleMax);
        }
    }

    // ── 침 공격 ──────────────────────────────────────────────────────────────
    void StartShoot()
    {
        _attackTimer = shootCooldown;
        _state       = State.Shoot;
        _spiderAnim?.TriggerShoot();
        PlaySfx(sfxShoot);

        if (_spiderBulletVisual != null) _spiderBulletVisual.SetActive(true);

        Invoke(nameof(ActivateShootHit), shootHitDelay);
        Invoke(nameof(EndShoot), shootStateDuration);
    }

    void ActivateShootHit() { if (!_isDead) EnableAttackHitBox(); }

    void EndShoot()
    {
        if (_spiderBulletVisual != null) _spiderBulletVisual.SetActive(false);
        if (_state != State.Shoot) return;
        _state     = State.Chase;
        _stunTimer = postActionStun;
    }

    // ── 피격 / 사망 ──────────────────────────────────────────────────────────
    public override void TakeDamage(int amount, float attackerX = 0f)
    {
        if (_isDead) return;
        CancelInvoke(nameof(ActivateShootHit));
        CancelInvoke(nameof(EndShoot));
        if (_spiderBulletVisual != null) _spiderBulletVisual.SetActive(false);

        _state = State.Hit;
        _spiderAnim?.PlayHit();
        base.TakeDamage(amount, attackerX);
        if (!_isDead) Invoke(nameof(RecoverFromHit), hitStateDuration);
    }

    void RecoverFromHit()
    {
        if (!_isDead && _state == State.Hit) _state = State.Chase;
    }

    protected override void Die()
    {
        CancelInvoke();
        if (_spiderBulletVisual != null) _spiderBulletVisual.SetActive(false);
        _spiderAnim?.PlayDead();
        base.Die();
    }
}
