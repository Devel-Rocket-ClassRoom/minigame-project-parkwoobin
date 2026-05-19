using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EnemyAnimationController : MonoBehaviour
{
    Animator _anim;

    static readonly int H_IsWalking = Animator.StringToHash("isWalking");
    static readonly int H_IsRunning = Animator.StringToHash("isRunning");
    static readonly int H_IsFalling = Animator.StringToHash("isFalling");

    static readonly int H_JumpTrigger   = Animator.StringToHash("Jump");
    static readonly int H_AttackTrigger = Animator.StringToHash("Attack");

    static readonly int H_LandState = Animator.StringToHash("Land");
    static readonly int H_HitState  = Animator.StringToHash("Hit");
    static readonly int H_WallState = Animator.StringToHash("Wall");
    static readonly int H_DeadState = Animator.StringToHash("Dead");

    void Awake() => _anim = GetComponent<Animator>();

    public void UpdateState(bool isMoving, bool isRunning, bool isFalling)
    {
        _anim.SetBool(H_IsWalking, isMoving && !isRunning);
        _anim.SetBool(H_IsRunning, isRunning);
        _anim.SetBool(H_IsFalling, isFalling);
    }

    public void TriggerJump()   => _anim.SetTrigger(H_JumpTrigger);
    public void TriggerLand()   => _anim.Play(H_LandState);
    public void TriggerAttack() => _anim.SetTrigger(H_AttackTrigger);
    public void PlayHit()       => _anim.Play(H_HitState);
    public void PlayWall()      => _anim.Play(H_WallState);
    public void PlayDead()      => _anim.Play(H_DeadState);
}
