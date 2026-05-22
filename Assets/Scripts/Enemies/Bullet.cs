using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] float speed = 12f;
    [SerializeField] int   damage = 1;
    [SerializeField] float lifetime = 3f;

    Rigidbody2D _rb;
    Collider2D  _col;
    GameObject  _shooter;
    int         _groundMask;

    static Sprite   _runtimeSprite;     // 모든 Bullet 인스턴스가 공유 (1회만 생성)
    static Material _runtimeMaterial;   // URP/2D 매젠타 fallback 방지용 기본 머티리얼

    void Awake()
    {
        _rb  = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();
        _rb.gravityScale = 0f;
        _rb.bodyType = RigidbodyType2D.Kinematic; // 직선 등속
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        if (_col != null) _col.isTrigger = true;
        _groundMask = LayerMask.GetMask("Ground");

        EnsureVisual();
    }

    /// <summary>SpriteRenderer가 비어있으면 코드로 머티리얼·스프라이트를 자동 생성 (URP 2D 매젠타 방지)</summary>
    void EnsureVisual()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        // 1) 머티리얼 누락 → URP 2D에서 매젠타로 보임. 적절한 sprite 셰이더를 자동 할당
        if (sr.sharedMaterial == null)
        {
            if (_runtimeMaterial == null)
            {
                // URP 2D → Universal Render Pipeline/2D/Sprite-Unlit-Default
                // Built-in → Sprites/Default
                Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    _runtimeMaterial = new Material(shader) { name = "BulletRuntimeMat" };
                }
            }
            if (_runtimeMaterial != null) sr.material = _runtimeMaterial;
        }

        // 2) Sprite도 없으면 코드로 노란 사각형 생성
        if (sr.sprite != null) return;
        if (_runtimeSprite == null)
        {
            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[64];
            for (int i = 0; i < 64; i++) pixels[i] = new Color32(255, 220, 60, 255);
            tex.SetPixels32(pixels);
            tex.Apply();
            _runtimeSprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f);
            _runtimeSprite.name = "BulletRuntimeSprite";
        }
        sr.sprite = _runtimeSprite;
    }

    /// <summary>외부에서 호출 — 방향, 발사자, (선택) 데미지 덮어쓰기</summary>
    public void Launch(Vector2 direction, GameObject shooter, int dmg = -1)
    {
        _shooter = shooter;
        if (dmg > 0) damage = dmg;

        Vector2 d = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        _rb.linearVelocity = d * speed;

        // 진행 방향으로 회전 (스프라이트가 옆을 향한다고 가정)
        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 발사자 본인/자식 무시
        if (_shooter != null)
        {
            if (other.gameObject == _shooter) return;
            if (other.transform.IsChildOf(_shooter.transform)) return;
        }
        // 다른 적과는 충돌 무시 (아군 친화 방지)
        if (other.GetComponentInParent<EnemyBase>() != null) return;

        // 플레이어 피격
        var player = other.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            player.TakeDamage(damage, transform.position.x);
            Destroy(gameObject);
            return;
        }

        // 지면/벽에 닿으면 소멸
        if (_groundMask != 0 && ((1 << other.gameObject.layer) & _groundMask) != 0)
            Destroy(gameObject);
    }
}
