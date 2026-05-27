using UnityEngine;

/// <summary>
/// 고양이 적 스포너.
/// Normal / Fast / Strong 3종 프리팹 중 셔플 랜덤 스폰.
/// </summary>
public class CatSpawner : EnemySpawnerBase
{
    [Header("Cat Prefabs")]
    [SerializeField] GameObject catNormalPrefab;
    [SerializeField] GameObject catFastPrefab;
    [SerializeField] GameObject catStrongPrefab;

    protected override GameObject[] GetPrefabs() =>
        new[] { catNormalPrefab, catFastPrefab, catStrongPrefab };
}
