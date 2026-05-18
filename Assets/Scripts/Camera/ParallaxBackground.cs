using UnityEngine;

/// <summary>
/// 카메라 이동에 따라 여러 레이어를 다른 속도로 스크롤하는 패럴랙스 배경.
/// 각 레이어에 SpriteRenderer를 붙인 자식 오브젝트를 등록하고
/// parallaxFactor 0=고정, 1=카메라와 동일속도 (실질적 고정)
/// 0.5=카메라 절반 속도 (중경), 0.2=먼 배경
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    [System.Serializable]
    public class Layer
    {
        public Transform target;          // 레이어 오브젝트
        [Range(0f, 1f)]
        public float parallaxFactor;  // 0=완전 고정, 1=카메라와 같이 이동
        public bool repeatX = true;  // X 방향 무한 반복
        [HideInInspector] public float spriteWidth;
        [HideInInspector] public float startX;
    }

    [SerializeField] Layer[] layers;

    Camera _cam;
    float _prevCamX;

    void Awake()
    {
        _cam = Camera.main;

        foreach (var layer in layers)
        {
            if (layer.target == null) continue;
            layer.startX = layer.target.position.x;

            // SpriteRenderer 너비 캐싱
            var sr = layer.target.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
                layer.spriteWidth = sr.sprite.bounds.size.x * layer.target.lossyScale.x;
            else
                layer.spriteWidth = 100f; // 폴백
        }

        _prevCamX = _cam.transform.position.x;
    }

    void LateUpdate()
    {
        float camX = _cam.transform.position.x;
        float delta = camX - _prevCamX;

        foreach (var layer in layers)
        {
            if (layer.target == null) continue;

            // 패럴랙스 이동
            float move = delta * layer.parallaxFactor;
            layer.target.position += new Vector3(move, 0f, 0f);

            // X 무한 반복 처리
            if (layer.repeatX && layer.spriteWidth > 0f)
            {
                float relX = layer.target.position.x - _cam.transform.position.x;
                if (Mathf.Abs(relX) >= layer.spriteWidth * 0.5f)
                {
                    float offset = relX > 0
                        ? -layer.spriteWidth
                        : layer.spriteWidth;
                    layer.target.position += new Vector3(offset, 0f, 0f);
                }
            }
        }

        _prevCamX = camX;
    }
}
