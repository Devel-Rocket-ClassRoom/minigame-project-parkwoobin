using UnityEngine;

/// <summary>
/// 게임 시작 시 레이어 간 충돌 규칙을 전역으로 설정합니다.
/// 씬에 오브젝트를 배치할 필요 없이 자동 실행됩니다.
/// </summary>
public static class PhysicsLayerSetup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Setup()
    {
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int itemLayer  = LayerMask.NameToLayer("Item");

        if (enemyLayer < 0 || itemLayer < 0) return;

        // 적이 아이템을 밀지 않도록
        Physics2D.IgnoreLayerCollision(enemyLayer, itemLayer, true);
    }
}
