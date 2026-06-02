using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 씬 내 진입점 + 스폰 박스.
/// 씬 시작 시 박스가 물리(Rigidbody2D)로 낙하한 뒤,
/// PlayerSpawner가 Open 애니메이션을 재생하고 플레이어를 등장시킨다.
/// </summary>
public class playerSpawnPoint : MonoBehaviour
{
    private static readonly Dictionary<string, playerSpawnPoint> _registry = new();

    [SerializeField] private string entryID;

    [Header("스폰 박스")]
    [Tooltip("박스 오브젝트의 Animator. 없으면 애니메이션 없이 바로 등장.")]
    [SerializeField] private Animator boxAnimator;
    [Tooltip("재생할 박스 열기 애니메이션 상태 이름")]
    [SerializeField] private string openStateName = "CatBoxOpen";
    [Tooltip("Open 애니메이션 재생 후 플레이어가 튀어나오기까지 대기 시간(초)")]
    [SerializeField] private float openDuration = 0.6f;

    public string EntryID => entryID;

    // ── 등록/해제 ──────────────────────────────────────────────────────────

    private void Awake()
    {
        if (string.IsNullOrEmpty(entryID))
        {
            Debug.LogWarning($"[SpawnPoint] entryID가 비어 있습니다: {name}", this);
            return;
        }
        _registry[entryID] = this;
    }

    private void OnDestroy()
    {
        if (!string.IsNullOrEmpty(entryID)
            && _registry.TryGetValue(entryID, out var sp) && sp == this)
        {
            _registry.Remove(entryID);
        }
    }

    // ── 외부 조회 ──────────────────────────────────────────────────────────

    /// <summary>등록된 진입점 인스턴스를 반환. 없으면 null.</summary>
    public static playerSpawnPoint Get(string id)
    {
        if (!string.IsNullOrEmpty(id) && _registry.TryGetValue(id, out var sp))
            return sp;
        return null;
    }

    /// <summary>등록된 진입점 위치를 반환. 없으면 null.</summary>
    public static Vector2? GetPosition(string id)
    {
        var sp = Get(id);
        return sp != null ? (Vector2?)sp.transform.position : null;
    }

    // ── 착지 감지 ──────────────────────────────────────────────────────────

    public event System.Action OnLanded;

    void OnCollisionEnter2D(Collision2D col) => OnLanded?.Invoke();

    // ── 박스 연출 ──────────────────────────────────────────────────────────

    /// <summary>Open 애니메이션을 재생하고 openDuration만큼 대기한다.</summary>
    public IEnumerator OpenBox()
    {
        if (boxAnimator != null)
            boxAnimator.Play(openStateName, 0);

        yield return new WaitForSeconds(openDuration);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
    }
#endif
}
