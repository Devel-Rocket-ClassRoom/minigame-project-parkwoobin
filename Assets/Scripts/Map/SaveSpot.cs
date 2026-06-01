using UnityEngine;

/// <summary>
/// 플레이어가 지나가면 게임을 저장하는 스팟.
/// 평소엔 "Flag_idle" 애니메이션, 플레이어 통과 시 "Flag" 애니메이션으로 전환.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SaveSpot : MonoBehaviour
{
    string idleStateName = "Flag_idle";
    string activeStateName = "Flag";

    bool _activated;
    Animator _anim;

    void Awake()
    {
        _anim = GetComponent<Animator>();
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_activated) return;
        if (!other.CompareTag("Player")) return;

        _activated = true;
        _anim?.Play(activeStateName);

        SaveManager.Instance?.AutoSave();
        Debug.Log($"[SaveSpot] {name} — 게임 저장 완료");
    }
}
