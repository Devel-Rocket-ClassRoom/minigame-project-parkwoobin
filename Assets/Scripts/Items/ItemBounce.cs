using UnityEngine;

/// <summary>
/// Ground 태그에 닿으면 아이템 비주얼이 위치 오프셋으로
/// 1차, 2차, 3차 점점 낮아지는 바운스 연출.
/// Rigidbody2D 물리 바운스 없음. 횡이동 물리는 그대로 유지.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ItemBounce : MonoBehaviour
{
    [Header("Visual Target")]
    [SerializeField] Transform visualRoot;
    // SpriteRenderer가 있는 자식 오브젝트를 넣는 걸 추천
    // 없으면 transform 자체 사용 (단, 물리 이동 간섭 최소화를 위해 자식 권장)

    [Header("Height Bounce")]
    [SerializeField] float bounceHeight = 0.25f;  // 첫 번째 바운스 높이
    [SerializeField] float heightDamping = 0.50f;  // 다음 바운스 높이 감소율 (0.5 = 절반)
    [SerializeField] float bounceDuration = 0.16f;  // 바운스 1회 시간(초)
    [SerializeField] int maxBounces = 3;      // 총 바운스 횟수

    Rigidbody2D _rb;
    float _baseY;      // 착지 시 기준 Y (localPosition.y)

    bool _bouncing;
    bool _hasLanded;        // 시퀀스 완료 후 재발동 방지
    float _bounceT;
    int _bounceIndex;      // 현재 바운스 회차 (0-based)

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (visualRoot == null)
            visualRoot = transform;
    }

    void OnEnable()
    {
        _bouncing = false;
        _hasLanded = false;
        _bounceT = 0f;
        _bounceIndex = 0;

        // 비주얼 Y를 기준값으로 복귀
        var p = visualRoot.localPosition;
        visualRoot.localPosition = new Vector3(p.x, 0f, p.z);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (_bouncing || _hasLanded) return;   // 이미 연출 중이거나 완료
        if (!IsGroundHit(col)) return;

        // 착지 순간의 Y를 기준으로 삼음
        _baseY = visualRoot.localPosition.y;
        _bounceIndex = 0;
        _bounceT = 0f;
        _bouncing = true;
    }

    void Update()
    {
        if (!_bouncing) return;

        _bounceT += Time.deltaTime / bounceDuration;
        float t = Mathf.Clamp01(_bounceT);

        // 회차가 올라갈수록 높이 감소 (0.5^n)
        float height = bounceHeight * Mathf.Pow(heightDamping, _bounceIndex);
        float offsetY = Mathf.Sin(t * Mathf.PI) * height;

        // X·Z는 물리에 맡기고 Y만 오프셋 적용
        var pos = visualRoot.localPosition;
        visualRoot.localPosition = new Vector3(pos.x, _baseY + offsetY, pos.z);

        if (t >= 1f)
        {
            _bounceIndex++;

            if (_bounceIndex >= maxBounces)
            {
                // 시퀀스 완전 종료 — 다시는 발동되지 않음
                _bouncing = false;
                _hasLanded = true;
                pos = visualRoot.localPosition;
                visualRoot.localPosition = new Vector3(pos.x, _baseY, pos.z);
                return;
            }

            _bounceT = 0f;   // 다음 바운스 시작
        }
    }

    bool IsGroundHit(Collision2D col)
    {
        if (!col.gameObject.CompareTag("Ground")) return false;
        foreach (var c in col.contacts)
            if (c.normal.y > 0.5f) return true;
        return false;
    }
}
