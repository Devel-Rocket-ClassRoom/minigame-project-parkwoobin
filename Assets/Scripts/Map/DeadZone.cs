using UnityEngine;

/// <summary>
/// 플레이어가 이 Trigger에 닿으면 HP와 배고픔을 0으로 만들어 사망 처리한다.
/// Collider2D를 isTrigger로 설정하고 이 컴포넌트를 붙이면 된다.
/// </summary>
public class DeadZone : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[DeadZone] 충돌: {other.gameObject.name}");

        var player = other.GetComponentInParent<PlayerController>();
        if (player == null) { Debug.Log("[DeadZone] PlayerController 없음 — 무시"); return; }

        Debug.Log("[DeadZone] 플레이어 감지 → Die() 호출");

        // 배고픔 0
        var hunger = FindFirstObjectByType<HungerSystem>();
        if (hunger != null) hunger.SetHunger(0f);

        // HP 0으로 설정 후 직접 사망 처리
        player.SetHp(0, player.MaxHp);
        player.Die();

        // 1초 뒤 패널 표시
        var panel = FindFirstObjectByType<GameOverPanelController>();
        if (panel != null) panel.ShowAfter(1f);
    }
}
