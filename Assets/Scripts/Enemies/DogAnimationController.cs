using UnityEngine;

/// <summary>
/// Dog1 / Dog2 Animator 전용 컨트롤러.
/// 파라미터: isWalking(bool), Attack(trigger)
/// 상태명: Idle, Walk, Attack, Hurt, Death
/// </summary>
[RequireComponent(typeof(Animator))]
public class DogAnimationController : MonoBehaviour
{
    Animator _anim;

    // ── 파라미터 해시 ────────────────────────────────────────────────────────
    static readonly int H_IsWalking     = Animator.StringToHash("isWalking");
    static readonly int H_AttackTrigger = Animator.StringToHash("Attack");

    // ── 상태 이름 해시 (Play 직접 전환용) ────────────────────────────────────
    static readonly int H_HurtState  = Animator.StringToHash("Hurt");
    static readonly int H_DeathState = Animator.StringToHash("Death");

    void Awake() => _anim = GetComponent<Animator>();

    // ── 매 프레임 이동 상태 갱신 ─────────────────────────────────────────────
    /// <param name="isMoving">이동 중이면 true → Walk, 아니면 → Idle</param>
    public void UpdateState(bool isMoving)
        => _anim.SetBool(H_IsWalking, isMoving);

    // ── 트리거 ───────────────────────────────────────────────────────────────
    /// AnyState → Attack 전환 (Attack trigger)
    public void TriggerAttack() => _anim.SetTrigger(H_AttackTrigger);

    // ── 직접 전환 ────────────────────────────────────────────────────────────
    /// 피격 즉시 Hurt 상태 재생
    public void PlayHurt()  => _anim.Play(H_HurtState);

    /// 사망 즉시 Death 상태 재생
    public void PlayDeath() => _anim.Play(H_DeathState);
}
