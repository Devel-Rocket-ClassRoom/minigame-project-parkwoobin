using UnityEngine;

/// <summary>
/// target 위치를 확인하여 계산한 offset 만큼 더해주고 카메라가 이동 가능한 범위까지 Clamp하여 현재 카메라 위치에서 목표 위치까지
/// 부드럽게 이동하는 스크립트
/// </summary>


public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float smoothSpeed = 5f;
    [Tooltip("위로 올라갈 때(점프) Y 추적 속도. 낮을수록 점프 시 카메라가 덜 따라옴")]
    [SerializeField] float upSmoothSpeed = 2f;
    [SerializeField] Vector3 offset = new Vector3(0f, 0.2f, -10f);
    [SerializeField] float minX = float.MinValue;
    [SerializeField] float maxX = float.MaxValue;
    [SerializeField] float minY = float.MinValue;
    [SerializeField] float maxY = float.MaxValue;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;
        desired.x = Mathf.Clamp(desired.x, minX, maxX);
        desired.y = Mathf.Clamp(desired.y, minY, maxY);

        // Y: 위로 갈 때는 느리게, 아래로/수평은 기본 속도
        float ySpeed = desired.y > transform.position.y ? upSmoothSpeed : smoothSpeed;
        float newX = Mathf.Lerp(transform.position.x, desired.x, smoothSpeed * Time.deltaTime);
        float newY = Mathf.Lerp(transform.position.y, desired.y, ySpeed * Time.deltaTime);
        transform.position = new Vector3(newX, newY, desired.z);
    }

    public void SetBounds(float xMin, float xMax, float yMin, float yMax)
    {
        minX = xMin; maxX = xMax;
        minY = yMin; maxY = yMax;
    }

    // X 경계만 설정 (Y는 기존 값 유지)
    public void SetBounds(float xMin, float xMax)
    {
        minX = xMin; maxX = xMax;
    }
}
