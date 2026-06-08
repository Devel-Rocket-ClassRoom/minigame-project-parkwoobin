using UnityEngine;

/// <summary>
/// 개 적 스포너.
/// Dog1(Yellow) / Dog2(Black) 2종 프리팹 중 셔플 랜덤 스폰.
/// </summary>
public class DogSpawner : EnemySpawnerBase
{
    [Header("Dog Prefabs")]
    [SerializeField] GameObject dog1Prefab;
    [SerializeField] GameObject dog2Prefab;

    protected override GameObject[] GetPrefabs() =>
        new[] { dog1Prefab, dog2Prefab };
}
