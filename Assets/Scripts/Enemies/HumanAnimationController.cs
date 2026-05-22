using UnityEngine;

[RequireComponent(typeof(Animator))]
public class HumanAnimationController : MonoBehaviour
{
    Animator _anim;

    // ── 파라미터 해시 ─────────────────────────────────────────────────────
    static readonly int H_IsWalking    = Animator.StringToHash("isWalking");
    static readonly int H_IsRunning    = Animator.StringToHash("isRunning");
    static readonly int H_IsFalling    = Animator.StringToHash("isFalling");
    static readonly int H_Jump         = Animator.StringToHash("Jump");
    static readonly int H_Fight        = Animator.StringToHash("Fight");
    static readonly int H_FightVariant = Animator.StringToHash("FightVariant"); // 0/1/2 (Human2용)
    static readonly int H_Shot         = Animator.StringToHash("Shot");
    static readonly int H_Hurt         = Animator.StringToHash("Hurt");
    static readonly int H_Dead         = Animator.StringToHash("Dead");

    void Awake() => _anim = GetComponent<Animator>();

    /// <summary>매 프레임 이동/낙하 상태 갱신</summary>
    public void UpdateState(bool isMoving, bool isRunning, bool isFalling)
    {
        if (_anim == null) return;
        _anim.SetBool(H_IsWalking, isMoving && !isRunning);
        _anim.SetBool(H_IsRunning, isRunning);
        _anim.SetBool(H_IsFalling, isFalling);
    }

    public void TriggerJump() { if (_anim != null) _anim.SetTrigger(H_Jump); }

    /// <summary>variant: 0/1/2 — Human2의 Fight 1/2/3 분기용. 다른 Human은 무시</summary>
    public void TriggerFight(int variant = 0)
    {
        if (_anim == null) return;
        _anim.SetInteger(H_FightVariant, variant);
        _anim.SetTrigger(H_Fight);
    }

    public void TriggerShot() { if (_anim != null) _anim.SetTrigger(H_Shot); }
    public void PlayHurt()    { if (_anim != null) _anim.SetTrigger(H_Hurt); }
    public void PlayDead()    { if (_anim != null) _anim.SetTrigger(H_Dead); }
}
