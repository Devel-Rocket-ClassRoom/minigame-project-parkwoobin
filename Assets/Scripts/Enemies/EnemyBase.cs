using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum EnemyType { Normal, Fast, Strong }

[RequireComponent(typeof(Rigidbody2D))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] protected int maxHp = 3;
    [SerializeField] protected float moveSpeed = 3f;
    [SerializeField] protected int attackPower = 1;

    [Header("Attack HitBox")]
    [SerializeField] GameObject _attackHitBox;
    [SerializeField] float _hitBoxDuration = 0.2f;

    [Header("Movement Constraints")]
    [Tooltip("적이 올라설 수 있는 레이어. 비어있으면 제한 없음.\n여기에 없는 레이어 바닥으로는 이동하지 않음.")]
    [SerializeField] LayerMask validGroundLayers;

    // 스포너에서 SetCanJump()로 설정
    protected bool canJump = true;
    public void SetCanJump(bool value) => canJump = value;

    [Header("Hurt Knockback")]
    [Tooltip("피격 시 뒤로 밀려나는 거리(unit)")]
    [SerializeField] float knockbackDistance = 0.1f;

    [Header("Item Drop")]
    [Tooltip("처치 시 드롭될 수 있는 아이템 프리팹들. 매번 무작위로 선택됨")]
    [SerializeField] GameObject[] dropItemPrefabs;
    [Tooltip("드롭 개수별 가중치 [0개, 1개, 2개, 3개]. 합은 자동 정규화. 0개가 가장 흔하고 3개가 가장 드물게 설정 권장")]
    [SerializeField] float[] dropCountWeights = { 55f, 28f, 12f, 5f };
    [Tooltip("드롭 아이템 위로 튀어오르는 속도 범위 (Y)")]
    [SerializeField] Vector2 dropLaunchUp = new Vector2(3f, 5f);
    [Tooltip("드롭 아이템 좌우 튀는 속도 범위 (X)")]
    [SerializeField] Vector2 dropLaunchSide = new Vector2(-1.5f, 1.5f);
    [Tooltip("드롭 스폰 위치 미세 분산(±)")]
    [SerializeField] float dropSpawnSpread = 0.08f;

    protected Rigidbody2D _rb;
    protected EnemyAnimationController _anim;
    protected Collider2D _col;
    static Plane[] _cameraPlanes = new Plane[6];

    protected bool _isGrounded;
    protected bool _isOnWall;
    protected bool _facingRight = true;
    protected bool _isDead;
    float _flipCooldown;
    Coroutine _hitBoxCoroutine;

    int _hp;
    protected int _groundMask;

    Vector3 _spawnPoint;

    AudioSource _sfxSource;

    public System.Action<int, int> OnHealthChanged;

    static readonly ContactPoint2D[] _contacts = new ContactPoint2D[8];

    public float MoveSpeed => moveSpeed;
    public int AttackPower => attackPower;

    /// <summary>
    /// 진행 방향 앞 발 아래에 validGroundLayers에 속하는 바닥이 있는지 확인한다.
    /// validGroundLayers가 0(Nothing)이면 항상 true(제한 없음).
    /// </summary>
    protected bool IsValidGroundAhead(float dirX)
    {
        if (validGroundLayers == 0) return true;

        // 발 위치에서 한 발짝 앞 지점 아래로 레이캐스트
        float feetY = _col != null ? _col.bounds.min.y : transform.position.y - 0.5f;
        float checkX = transform.position.x + Mathf.Sign(dirX) * 0.4f;
        var hit = Physics2D.Raycast(new Vector2(checkX, feetY), Vector2.down, 1.2f, _groundMask);

        if (hit.collider == null) return false; // 앞에 바닥 자체가 없음 → 낭떠러지
        return (validGroundLayers & (1 << hit.collider.gameObject.layer)) != 0;
    }

    /// <summary>스포너가 인스턴스 생성 후 스폰 위치를 알려준다.</summary>
    public void SetSpawnPoint(Vector3 pos) => _spawnPoint = pos;

    /// <summary>DeadZone 진입 시 스폰 위치로 복귀 + 상태 초기화.</summary>
    public void ResetToSpawn()
    {
        StopAllCoroutines();

        // 위치 복귀
        transform.position = _spawnPoint;

        // 물리 복원
        _rb.bodyType = RigidbodyType2D.Dynamic;
        _rb.linearVelocity = Vector2.zero;

        // 스탯 초기화
        _hp = maxHp;
        _isDead = false;
        _flipCooldown = 0f;
        OnHealthChanged?.Invoke(_hp, maxHp);

        // 히트박스 끄기
        _attackHitBox?.SetActive(false);
        _hitBoxCoroutine = null;

        // 서브클래스 초기화
        OnRespawn();
    }

    /// <summary>서브클래스에서 재정의해 상태머신 등 추가 초기화.</summary>
    protected virtual void OnRespawn() { }

    protected virtual void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<EnemyAnimationController>();
        _col = GetComponent<Collider2D>();
        _hp = maxHp;
        _spawnPoint = transform.position; // 씬 직접 배치 시 기본값

        // Ground 레이어가 정의되지 않은 프로젝트 대비: 자기 레이어 제외 전체로 fallback
        _groundMask = LayerMask.GetMask("Ground");
        if (_groundMask == 0)
            _groundMask = ~(1 << gameObject.layer);

        // Inspector 미연결 시 "HitBox" 이름의 자식을 자동 탐색
        if (_attackHitBox == null)
            _attackHitBox = transform.Find("HitBox")?.gameObject;
        _attackHitBox?.SetActive(false);
    }

    protected virtual void Update()
    {
        if (_isDead) return;
        if (_flipCooldown > 0f) _flipCooldown -= Time.deltaTime;

        _isGrounded = false;
        _isOnWall = false;
        int cnt = _rb.GetContacts(_contacts);
        for (int i = 0; i < cnt; i++)
        {
            int layer = _contacts[i].collider.gameObject.layer;
            if (((1 << layer) & _groundMask) == 0) continue;
            if (_contacts[i].normal.y > 0.8f) _isGrounded = true;
            if (Mathf.Abs(_contacts[i].normal.x) > 0.8f) _isOnWall = true;
        }
    }

    public virtual void TakeDamage(int amount, float attackerX = 0f)
    {
        if (_isDead) return;
        _hp -= amount;
        OnHealthChanged?.Invoke(_hp, maxHp);
        if (_hp <= 0) Die();
        else
        {
            _anim?.PlayHit();
            float dir = transform.position.x >= attackerX ? 1f : -1f;
            transform.position += new Vector3(dir * knockbackDistance, 0f, 0f);
        }
    }

    protected virtual void Die()
    {
        _isDead = true;
        _rb.linearVelocity = Vector2.zero;
        _rb.bodyType = RigidbodyType2D.Static;
        _anim?.PlayDead();
        DropItems();
        // TODO: 점수 처리
        Destroy(gameObject, 1f);
    }

    /// <summary>처치 시 아이템 드롭. 가중치에 따라 0~3개 무작위로 튀어나옴.</summary>
    protected virtual void DropItems()
    {
        if (dropItemPrefabs == null || dropItemPrefabs.Length == 0) return;

        int count = PickDropCount();
        for (int i = 0; i < count; i++)
            SpawnOneDropItem();
    }

    int PickDropCount()
    {
        if (dropCountWeights == null || dropCountWeights.Length == 0) return 0;
        float total = 0f;
        for (int i = 0; i < dropCountWeights.Length; i++)
            if (dropCountWeights[i] > 0f) total += dropCountWeights[i];
        if (total <= 0f) return 0;

        float r = Random.value * total;
        float acc = 0f;
        for (int i = 0; i < dropCountWeights.Length; i++)
        {
            if (dropCountWeights[i] <= 0f) continue;
            acc += dropCountWeights[i];
            if (r < acc) return i;
        }
        return 0;
    }

    void SpawnOneDropItem()
    {
        var prefab = dropItemPrefabs[Random.Range(0, dropItemPrefabs.Length)];
        if (prefab == null) return;

        Vector3 spawnPos = transform.position + new Vector3(
            Random.Range(-dropSpawnSpread, dropSpawnSpread),
            Random.Range(-dropSpawnSpread, dropSpawnSpread),
            0f);

        var instance = Instantiate(prefab, spawnPos, Quaternion.identity);
        var rb = instance.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            float vy = Random.Range(dropLaunchUp.x, dropLaunchUp.y);
            float vx = Random.Range(dropLaunchSide.x, dropLaunchSide.y);
            rb.linearVelocity = new Vector2(vx, vy);
        }
    }

    protected void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;
        if (Camera.main != null)
        {
            GeometryUtility.CalculateFrustumPlanes(Camera.main, _cameraPlanes);
            var bounds = _col != null ? _col.bounds : new Bounds(transform.position, Vector3.one);
            if (!GeometryUtility.TestPlanesAABB(_cameraPlanes, bounds)) return;
        }
        if (_sfxSource == null)
        {
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
        }
        float vol = AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : 1f;
        _sfxSource.PlayOneShot(clip, vol);
    }

    protected void Flip()
    {
        _facingRight = !_facingRight;
        var s = transform.localScale;
        s.x *= -1f;
        transform.localScale = s;
    }

    protected void FaceToward(float targetX)
    {
        if (_flipCooldown > 0f) return;
        float dir = targetX - transform.position.x;
        if (dir > 0.4f && !_facingRight) { Flip(); _flipCooldown = 0.8f; }
        else if (dir < -0.4f && _facingRight) { Flip(); _flipCooldown = 0.8f; }
    }

    protected void EnableAttackHitBox()
    {
        if (_attackHitBox == null) return;
        if (_hitBoxCoroutine != null) StopCoroutine(_hitBoxCoroutine);
        _attackHitBox.SetActive(true);
        _hitBoxCoroutine = StartCoroutine(AttackHitBoxRoutine());
    }

    IEnumerator AttackHitBoxRoutine()
    {
        var col = _attackHitBox.GetComponent<BoxCollider2D>();
        var hitPlayers = new HashSet<PlayerController>();
        float elapsed = 0f;

        while (elapsed < _hitBoxDuration)
        {
            if (col != null)
            {
                var hits = Physics2D.OverlapBoxAll(col.bounds.center, col.bounds.size, 0f);
                foreach (var h in hits)
                {
                    if (h.gameObject == gameObject) continue;
                    var player = h.GetComponentInParent<PlayerController>();
                    if (player != null && hitPlayers.Add(player))
                        player.TakeDamage(attackPower, transform.position.x);
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        _attackHitBox.SetActive(false);
        _hitBoxCoroutine = null;
    }
}
