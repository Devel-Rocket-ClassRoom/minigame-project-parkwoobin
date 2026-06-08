using UnityEngine;

/// <summary>
/// 사람 적 스포너.
/// Human1 / Human2 / Human3 3종 프리팹 중 셔플 랜덤 스폰.
/// </summary>
public class HumanSpawner : EnemySpawnerBase
{
    [Header("Human Prefabs")]
    [SerializeField] GameObject human1Prefab;
    [SerializeField] GameObject human2Prefab;
    [SerializeField] GameObject human3Prefab;

    protected override GameObject[] GetPrefabs() =>
        new[] { human1Prefab, human2Prefab, human3Prefab };
}
