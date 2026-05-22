using UnityEngine;

/// <summary>
/// 개 적 AI.
/// 스폰 위치 기준으로 patrolRange 범위를 순찰하다가
/// 플레이어가 detectionRange 안에 들어오면 추격 → 공격.
/// 점프 없음. CatEnemy보다 빠른 속도.
///
/// [변형]
///   Yellow — 기본형. HP 3 / 속도 3.0 / 공격력 1
///   Black  — 강화형. HP 5 / 속도 4.5 / 공격력 2
///
/// [프리팹 설정]
///   - 이 컴포넌트 + DogAnimationController + Animator(Dog1 or Dog2.controller)
///   - Rigidbody2D (Dynamic)
///   - Collider2D (Enemy 레이어)
///   - 자식 오브젝트 "HitBox": BoxCollider2D (Is Trigger = true)
///   - EnemyHealthBar (선택)
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class DogEnemy : EnemyBase
{
    // ── 변형 ─────────────────────────────────────────────────────────────────
    public enum DogVariant { Yellow, Black }

    [Header("Variant")]
    [SerializeField] DogVariant dogType = DogVariant.Yellow;

    // ── 순찰 ─────────────────────────────────────────────────────────────────
    [Header("Patrol")]
    [SerializeField] float patrolRange = 4f;
    [SerializeField] float patrolSpeed = 1.5f;
    [SerializeField] float idleMin = 0.5f;
    [SerializeField] float idleMax = 2.0f;

    // ── 감지 / 추격 ──────────────────────────────────────────────────────────
    [Header("Detect / Chase")]
    [SerializeField] float detectionRange = 7f;
    [Tooltip("스폰 위치에서 플레이어가 이만큼 벗어나면 추격 포기 후 복귀")]
    [SerializeField] float leashRange = 9f;

    // ── 공격 ─────────────────────────────────────────────────────────────────
    [Header("Attack")]
    [SerializeField] float attackRange = 1.2f;
    [SerializeField] float attackCooldown = 1.2f;
    [Tooltip("Attack 클립 길이(초). 6fps 4프레임 = 0.667s")]
    [SerializeField] float attackClipLength = 0.667f;

    // ── 피격 ─────────────────────────────────────────────────────────────────
    [Header("Hit Recovery")]
    [SerializeField] float hitRecoveryTime = 0.3f;

    // ── 내부 상태 ────────────────────────────────────────────────────────────
    enum State { Idle, Patrol, Chase, Attack, Hit }

    DogAnimationController _dogAnim;
    BoxCollider2D _hitBoxCol;   // 자식 HitBox의 BoxCollider2D
    Transform _player;
    PlayerController _playerCtrl;

    State _state = State.Idle;
    float _idleTimer;
    float _attackTimer;
    float _patrolOriginX;
    int _patrolDir = 1;

    // ── 초기화 ───────────────────────────────────────────────────────────────
    protected override void Awake()
    {
        // 변형별 기본 스탯
        // Yellow: 모든 공격력 1, 기본 속도
        // Black : 속도·공격력 모두 Yellow보다 강함
        (maxHp, moveSpeed, attackPower) = dogType switch
        {
            DogVariant.Black => (3, 4.5f, 2),
            _ => (5, 3.0f, 1),   // Yellow (기본)
        };
        base.Awake();   // _rb, _col, _groundMask 세팅

        _dogAnim = GetComponent<DogAnimationController>();

        // HitBox 자식에서 BoxCollider2D 캐싱
        var hitBoxObj = transform.Find("HitBox");
        if (hitBoxObj != null)
            _hitBoxCol = hitBoxObj.GetComponent<BoxCollider2D>();

        _patrolOriginX = transform.position.x;
        _idleTimer = Random.Range(idleMin, idleMax);
    }

    void Start()
    {
        var p = GameObject.FindWithTag("Player");
        if (p == null) return;
        _player = p.transform;
        _playerCtrl = p.GetComponent<PlayerController>();
    }

    // ── 업데이트 ─────────────────────────────────────────────────────────────
    protected override void Update()
    {
        base.Update();   // _isGrounded, _isOnWall 갱신
        if (_isDead) return;

        _attackTimer -= Time.deltaTime;

        UpdateAI();

        // 이동 중이면 Walk, 정지면 Idle
        bool moving = Mathf.Abs(_rb.linearVelocity.x) > 0.1f;
        _dogAnim?.UpdateState(moving);
    }

    // ── AI 상태머신 ──────────────────────────────────────────────────────────
    void UpdateAI()
    {
        if (_player == null) return;

        float dx = _player.position.x - transform.position.x;
        float dist = Mathf.Abs(dx);
        float dy = Mathf.Abs(_player.position.y - transform.position.y);

        // 플레이어가 스폰 기준 활동 범위 안에 있는지
        bool playerInLeash = Mathf.Abs(_player.position.x - _patrolOriginX) < leashRange;

        switch (_state)
        {
            // ── 대기 ────────────────────────────────────────────────────────
            case State.Idle:
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                _idleTimer -= Time.deltaTime;
                if (_idleTimer <= 0f) _state = State.Patrol;
                if (dist < detectionRange && playerInLeash) _state = State.Chase;
                break;

            // ── 순찰 ────────────────────────────────────────────────────────
            case State.Patrol:
                MovePatrol();
                if (dist < detectionRange && playerInLeash) _state = State.Chase;
                break;

            // ── 추격 ────────────────────────────────────────────────────────
            case State.Chase:
                FaceToward(_player.position.x);

                // 활동 범위 벗어나면 복귀
                if (!playerInLeash)
                {
                    _state = State.Patrol;
                    break;
                }

                // 공격 사거리 안에 들어오면 공격
                if (dist <= attackRange && dy < 1.0f)
                {
                    _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                    if (_attackTimer <= 0f) StartAttack();
                    break;
                }

                // 추격 이동
                float dir = dx > 0f ? 1f : -1f;
                _rb.linearVelocity = new Vector2(dir * MoveSpeed, _rb.linearVelocity.y);

                // 너무 멀어지면 포기
                if (dist > detectionRange * 1.5f)
                {
                    _patrolOriginX = transform.position.x;
                    _state = State.Idle;
                    _idleTimer = Random.Range(idleMin, idleMax);
                }
                break;

            // ── 공격 중 ─────────────────────────────────────────────────────
            case State.Attack:
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                break;

            // ── 피격 경직 ────────────────────────────────────────────────────
            case State.Hit:
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                break;
        }
    }

    // ── 순찰 이동 ────────────────────────────────────────────────────────────
    void MovePatrol()
    {
        float targetX = _patrolOriginX + _patrolDir * patrolRange;
        FaceToward(targetX);
        _rb.linearVelocity = new Vector2(_patrolDir * patrolSpeed, _rb.linearVelocity.y);

        // 끝 지점 도달 시 방향 반전 후 대기
        if (Mathf.Abs(transform.position.x - targetX) < 0.2f)
        {
            _patrolDir *= -1;
            _state = State.Idle;
            _idleTimer = Random.Range(idleMin, idleMax);
        }
    }

    // ── 공격 시작 ────────────────────────────────────────────────────────────
    void StartAttack()
    {
        _attackTimer = attackCooldown;
        _state = State.Attack;
        _dogAnim?.TriggerAttack();
        // Attack 클립 재생 완료 후 Chase로 복귀
        Invoke(nameof(EndAttackState), attackClipLength);
    }

    void EndAttackState()
    {
        if (_state == State.Attack) _state = State.Chase;
    }

    // ── Animation Event ──────────────────────────────────────────────────────
    /// <summary>
    /// Attack 클립 2프레임(time = 0.333s) 시점에 Unity가 호출.
    /// HitBox 범위 안에 플레이어가 있으면 데미지 적용.
    /// </summary>
    public void OnAttackHitFrame()
    {
        if (_isDead || _player == null) return;
        if (!PlayerInHitBox()) return;
        _playerCtrl?.TakeDamage(attackPower, transform.position.x);
    }

    // ── HitBox 범위 체크 (CatEnemy 동일 방식) ────────────────────────────────
    /// <summary>HitBox BoxCollider2D의 월드 영역 안에 플레이어가 있으면 true.</summary>
    bool PlayerInHitBox()
    {
        if (_hitBoxCol == null || _player == null) return false;

        // 비활성 콜라이더는 bounds가 (0,0,0) → offset/size로 직접 월드 좌표 계산
        float signX = Mathf.Sign(transform.lossyScale.x);
        Vector2 offset = new Vector2(_hitBoxCol.offset.x * signX, _hitBoxCol.offset.y);
        Vector2 center = (Vector2)_hitBoxCol.transform.position + offset;
        Vector2 size = new Vector2(
            _hitBoxCol.size.x * Mathf.Abs(_hitBoxCol.transform.lossyScale.x),
            _hitBoxCol.size.y * Mathf.Abs(_hitBoxCol.transform.lossyScale.y)
        );

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

    // ── 피격 / 사망 오버라이드 ───────────────────────────────────────────────
    /// <summary>
    /// EnemyBase.TakeDamage의 _anim?.PlayHit()은 null이라 no-op.
    /// Dog 전용 Hurt 애니메이션을 먼저 재생한 뒤 base로 HP 처리 위임.
    /// </summary>
    public override void TakeDamage(int amount, float attackerX = 0f)
    {
        if (_isDead) return;

        // 공격 Invoke가 걸려 있으면 취소 (피격 중 EndAttackState 방지)
        CancelInvoke(nameof(EndAttackState));

        _state = State.Hit;
        _dogAnim?.PlayHurt();

        // base: HP 감소, 넉백, _anim?.PlayHit()(null = no-op), 사망 시 Die() 호출
        base.TakeDamage(amount, attackerX);

        if (!_isDead)
            Invoke(nameof(RecoverFromHit), hitRecoveryTime);
    }

    void RecoverFromHit()
    {
        if (!_isDead && _state == State.Hit) _state = State.Chase;
    }

    /// <summary>EnemyBase.Die의 _anim?.PlayDead()는 null = no-op. Dog 전용 Death 재생.</summary>
    protected override void Die()
    {
        CancelInvoke(nameof(EndAttackState));
        CancelInvoke(nameof(RecoverFromHit));
        _dogAnim?.PlayDeath();
        base.Die();   // Rigidbody Static 전환, 아이템 드롭, Destroy
    }
}
