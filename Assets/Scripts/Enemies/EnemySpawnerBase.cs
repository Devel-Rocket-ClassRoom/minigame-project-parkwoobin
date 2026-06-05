using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 적 스포너 공통 기반 클래스.
/// 한 번에 1~N마리 스폰 → 전부 죽으면 → 다음 스폰.
/// MaxEnemyCount 총 횟수 소진 시 종료.
/// 셔플 방식으로 같은 종류가 연속으로 나오는 것을 방지.
/// </summary>
public abstract class EnemySpawnerBase : MonoBehaviour
{
    [Header("Enemy Settings")]
    [Tooltip("체크 해제 시 이 스포너에서 나오는 적은 점프하지 않음")]
    [SerializeField] private bool canJump = true;

    [Header("Spawn Settings")]
    [Tooltip("한 번에 스폰할 최소 마리 수")]
    private int spawnCountMin = 1;

    [Tooltip("한 번에 스폰할 최대 마리 수")]
    [SerializeField] private int spawnCountMax = 3;

    [Tooltip("몬스터가 죽고 다음 스폰 간격 최솟값 (초)")]
    [SerializeField] private float spawnIntervalMin = 1f;

    [Tooltip("몬스터가 죽고 다음 스폰 간격 최댓값 (초)")]
    [SerializeField] private float spawnIntervalMax = 3f;


    [Tooltip("총 스폰 횟수. 이만큼 나오고 스포너 종료")]
    [SerializeField] private int maxEnemyCount = 5;

    GameObject[] _shuffled;
    int _shuffleIndex;
    int _totalSpawned;

    void Start()
    {
        _shuffled = GetPrefabs();
        Shuffle(_shuffled);
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (_totalSpawned < maxEnemyCount)
        {
            // 이번 웨이브 스폰 수 결정 (남은 횟수 초과 안 되도록 클램프)
            int remaining = maxEnemyCount - _totalSpawned;
            int count = Mathf.Min(Random.Range(spawnCountMin, spawnCountMax + 1), remaining);

            // count마리 스폰
            var wave = new List<GameObject>(count);
            for (int i = 0; i < count; i++)
            {
                var prefab = NextPrefab();
                if (prefab == null) yield break;

                var enemy = Instantiate(prefab, transform.position, Quaternion.identity);
                var enemyBase = enemy.GetComponent<EnemyBase>();
                enemyBase?.SetSpawnPoint(transform.position);
                enemyBase?.SetCanJump(canJump);
                wave.Add(enemy);
                _totalSpawned++;
            }

            // 이번 웨이브 전부 죽을 때까지 대기
            yield return new WaitUntil(() => wave.TrueForAll(e => e == null));

            // 마지막 스폰이면 종료
            if (_totalSpawned >= maxEnemyCount) yield break;

            // 다음 웨이브 전 딜레이
            yield return new WaitForSeconds(Random.Range(spawnIntervalMin, spawnIntervalMax));
        }
    }

    // 셔플 배열에서 순서대로 꺼냄. 다 쓰면 다시 셔플
    GameObject NextPrefab()
    {
        if (_shuffled == null || _shuffled.Length == 0) return null;

        if (_shuffleIndex >= _shuffled.Length)
        {
            Shuffle(_shuffled);
            _shuffleIndex = 0;
        }

        return _shuffled[_shuffleIndex++];
    }

    // Fisher-Yates 셔플
    void Shuffle(GameObject[] arr)
    {
        for (int i = arr.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
    }

    /// <summary>스폰 가능한 프리팹 목록 반환. 하위 클래스에서 구현.</summary>
    protected abstract GameObject[] GetPrefabs();
}
