using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MinimapCamera : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float height = 15f;

    Camera _cam;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        // URP 자동 렌더 루프에서 제외 — 수동으로만 RT에 렌더
        _cam.enabled = false;
    }

    void LateUpdate()
    {
        if (target == null || _cam == null) return;
        transform.position = new Vector3(target.position.x, target.position.y + height, -10f);
        _cam.Render();
    }
}
