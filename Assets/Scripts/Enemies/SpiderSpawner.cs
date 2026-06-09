using UnityEngine;

/// <summary>
/// 거미 스포너. EnemySpawnerBase 상속.
/// Inspector에서 spiderPrefabs에 Spider 프리팹을 연결하면 됨.
/// </summary>
public class SpiderSpawner : EnemySpawnerBase
{
    [Header("Spider Prefabs")]
    [SerializeField] GameObject[] spiderPrefabs;

    protected override GameObject[] GetPrefabs() => spiderPrefabs;
}
