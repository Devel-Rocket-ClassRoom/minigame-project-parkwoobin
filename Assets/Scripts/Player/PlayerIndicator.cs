using UnityEngine;

/// <summary>
/// 플레이어 머리 위에 떠 있는 인디케이터 구현
/// </summary>

public class PlayerIndicator : MonoBehaviour
{
    public float amplitude = 0.2f;
    public float frequency = 2f;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.localPosition;
    }

    void Update()
    {
        transform.localPosition = startPosition + new Vector3(0, Mathf.Sin(Time.time * frequency) * amplitude, 0);
    }
}
