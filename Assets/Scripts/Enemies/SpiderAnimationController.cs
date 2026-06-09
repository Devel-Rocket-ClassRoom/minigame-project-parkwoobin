using UnityEngine;

[RequireComponent(typeof(Animator))]
public class SpiderAnimationController : MonoBehaviour
{
    Animator _anim;

    static readonly int H_IsWalking   = Animator.StringToHash("isWalking");
    static readonly int H_Attack      = Animator.StringToHash("Attack");
    static readonly int H_Shoot       = Animator.StringToHash("Shoot");
    static readonly int H_Hit         = Animator.StringToHash("Hit");
    static readonly int H_Dead        = Animator.StringToHash("Dead");

    void Awake() => _anim = GetComponent<Animator>();

    public void SetWalking(bool walking) => _anim.SetBool(H_IsWalking, walking);
    public void TriggerAttack()          => _anim.SetTrigger(H_Attack);
    public void TriggerShoot()           => _anim.SetTrigger(H_Shoot);
    public void PlayHit()                => _anim.SetTrigger(H_Hit);
    public void PlayDead()               => _anim.SetTrigger(H_Dead);
}
