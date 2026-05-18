using UnityEngine;

/// <summary>
/// target 위치를 확인하여 계산한 offset 만큼 더해주고 카메라가 이동 가능한 범위까지 Clamp하여 현재 카메라 위치에서 목표 위치까지
/// 부드럽게 이동하는 스크립트
/// </summary>


public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float smoothSpeed = 5f;
    [SerializeField] Vector3 offset = new Vector3(0f, 1f, -10f);
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

        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
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
