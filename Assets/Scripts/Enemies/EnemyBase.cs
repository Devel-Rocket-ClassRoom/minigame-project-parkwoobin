using UnityEngine;

public enum EnemyType { Normal, Fast, Strong }

[RequireComponent(typeof(Rigidbody2D))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] protected EnemyType enemyType;
    [SerializeField] protected int   maxHp        = 3;
    [SerializeField] protected float moveSpeed    = 3f;
    [SerializeField] protected int   attackPower  = 1;

    protected Rigidbody2D              _rb;
    protected EnemyAnimationController _anim;
    protected Collider2D               _col;

    protected bool _isGrounded;
    protected bool _isOnWall;
    protected bool _facingRight = true;
    protected bool _isDead;
    float _flipCooldown;

    int _hp;
    int _groundMask;

    public System.Action<int, int> OnHealthChanged;

    static readonly ContactPoint2D[] _contacts = new ContactPoint2D[8];

    public EnemyType Type        => enemyType;
    public float     MoveSpeed   => moveSpeed;
    public int       AttackPower => attackPower;

    protected virtual void Awake()
    {
        _rb   = GetComponent<Rigidbody2D>();
        _anim = GetComponent<EnemyAnimationController>();
        _col  = GetComponent<Collider2D>();
        _hp   = maxHp;
        _groundMask = LayerMask.GetMask("Ground");
    }

    protected virtual void Update()
    {
        if (_isDead) return;
        if (_flipCooldown > 0f) _flipCooldown -= Time.deltaTime;

        _isGrounded = false;
        _isOnWall   = false;
        int cnt = _rb.GetContacts(_contacts);
        for (int i = 0; i < cnt; i++)
        {
            int layer = _contacts[i].collider.gameObject.layer;
            if (((1 << layer) & _groundMask) == 0) continue;
            if (_contacts[i].normal.y >  0.8f) _isGrounded = true;
            if (Mathf.Abs(_contacts[i].normal.x) > 0.8f) _isOnWall = true;
        }
    }

    public virtual void TakeDamage(int amount, float attackerX = 0f)
    {
        if (_isDead) return;
        _hp -= amount;
        OnHealthChanged?.Invoke(_hp, maxHp);
        Debug.Log($"[{gameObject.name}] HP: {_hp}/{maxHp}");
if (_hp <= 0) Die();
        else
        {
            _anim?.PlayHit();
            float dir = transform.position.x >= attackerX ? 1f : -1f;
            transform.position += new Vector3(dir * (3f / 32f), 0f, 0f);
        }
    }

    protected virtual void Die()
    {
        _isDead = true;
        _rb.linearVelocity = Vector2.zero;
        _rb.bodyType = RigidbodyType2D.Static;
        _anim?.PlayDead();
        // TODO: 아이템 드랍, 점수 처리
        Destroy(gameObject, 1f);
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
        if (dir > 0.4f && !_facingRight)       { Flip(); _flipCooldown = 0.8f; }
        else if (dir < -0.4f && _facingRight)  { Flip(); _flipCooldown = 0.8f; }
    }
}
