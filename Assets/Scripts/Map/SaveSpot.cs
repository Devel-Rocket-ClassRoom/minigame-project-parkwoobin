using UnityEngine;

/// <summary>
/// 플레이어가 지나가면 게임을 저장하는 스팟.
/// 평소엔 "Flag_Idle" 애니메이션, 플레이어 통과 시 "Flag" 애니메이션으로 전환.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SaveSpot : MonoBehaviour
{
    string activeStateName = "Flag";

    [SerializeField] AudioClip sfxSave;

    bool _activated;
    Animator _anim;
    AudioSource _sfxSource;

    void Awake()
    {
        _anim = GetComponent<Animator>();
        GetComponent<Collider2D>().isTrigger = true;

        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_activated) return;
        if (!other.CompareTag("Player")) return;

        _activated = true;
        _anim?.Play(activeStateName);

        if (sfxSave != null)
        {
            float vol = AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : 1f;
            _sfxSource.PlayOneShot(sfxSave, vol);
        }

        if (SaveManager.Instance == null)
        {
            Debug.LogWarning($"[SaveSpot] {name} — SaveManager 없음, 저장 실패");
            return;
        }

        var player = other.GetComponent<PlayerController>();
        // 저장 위치는 플레이어가 아닌 SaveSpot의 위치로 고정 (일관된 리스폰 포인트)
        SaveManager.Instance.AutoSaveAtPosition(player, transform.position);
        Debug.Log($"[SaveSpot] {name} — 게임 저장 완료 (pos={transform.position})");
    }
}
