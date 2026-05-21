using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class BoxController : MonoBehaviour
{
    [Header("Lock / Box Animators")]
    [SerializeField] Animator lockerAnimator;
    [SerializeField] GameObject lockerObject;
    [SerializeField] Animator boxAnimator;

    [Header("Item Drop")]
    [Tooltip("박스에서 튀어나올 수 있는 아이템 프리팹 목록")]
    [SerializeField] GameObject[] itemPrefabs;
    [Tooltip("아이템이 스폰될 위치. 비워두면 박스 자기 위치 사용")]
    [SerializeField] Transform spawnPoint;
    [Tooltip("튀어나올 아이템 개수")]
    [SerializeField] int itemCount = 3;
    [Tooltip("박스 열리는 애니메이션 후 아이템이 튀어나오기까지의 딜레이(초)")]
    [SerializeField] float spawnDelay = 1f;
    [Tooltip("아이템 사이의 스폰 간격(초). 줄줄이 터지는 느낌")]
    [SerializeField] float spawnInterval = 0.05f;
    [Tooltip("위로 튀어오르는 속도 (Y) 랜덤 범위")]
    [SerializeField] Vector2 launchUpForce = new Vector2(3f, 5f);
    [Tooltip("좌우로 흩어지는 속도 (X) 랜덤 범위")]
    [SerializeField] Vector2 launchSideForce = new Vector2(-1.5f, 1.5f);
    [Tooltip("스폰 위치 미세 분산(±)")]
    [SerializeField] float spawnSpread = 0.05f;

    bool _isPlayerInRange;
    bool _isOpened;

    void Update()
    {
        if (!_isPlayerInRange || _isOpened) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.fKey.wasPressedThisFrame) TryOpenBox();
    }

    void TryOpenBox()
    {
        var coinKey = CoinKeySystem.Instance;
        if (coinKey == null) return;

        if (!coinKey.UseKey())
        {
            // 키 없음 → Locked 애니메이션 (CrossFade로 항상 처음부터 재생)
            lockerAnimator?.CrossFade("Locked", 0f, 0, 0f);
            return;
        }

        // 키 있음 → 언락
        _isOpened = true;
        lockerAnimator?.CrossFade("UnLock", 0f, 0, 0f);
        boxAnimator?.CrossFade("Box_Open", 0f, 0, 0f);

        if (lockerObject != null)
            StartCoroutine(HideLockerAfterUnlock());

        StartCoroutine(SpawnItemsRoutine());
    }

    IEnumerator SpawnItemsRoutine()
    {
        if (itemPrefabs == null || itemPrefabs.Length == 0) yield break;

        if (spawnDelay > 0f)
            yield return new WaitForSeconds(spawnDelay);

        Vector3 origin = spawnPoint != null ? spawnPoint.position : transform.position;

        for (int i = 0; i < itemCount; i++)
        {
            SpawnOneItem(origin);
            if (spawnInterval > 0f)
                yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnOneItem(Vector3 origin)
    {
        var prefab = itemPrefabs[Random.Range(0, itemPrefabs.Length)];
        if (prefab == null) return;

        Vector3 spawnPos = origin + new Vector3(
            Random.Range(-spawnSpread, spawnSpread),
            Random.Range(-spawnSpread, spawnSpread),
            0f);

        var instance = Instantiate(prefab, spawnPos, Quaternion.identity);

        var rb = instance.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            float vy = Random.Range(launchUpForce.x, launchUpForce.y);
            float vx = Random.Range(launchSideForce.x, launchSideForce.y);
            rb.linearVelocity = new Vector2(vx, vy);
        }
    }

    IEnumerator HideLockerAfterUnlock()
    {
        // UnLock 상태로 전환될 때까지 대기
        yield return new WaitUntil(() =>
            lockerAnimator != null &&
            lockerAnimator.GetCurrentAnimatorStateInfo(0).IsName("UnLock"));

        float length = lockerAnimator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(length);

        lockerObject.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _isPlayerInRange = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _isPlayerInRange = false;
    }
}
