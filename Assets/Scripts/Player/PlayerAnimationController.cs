using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    Animator _anim;
    Rigidbody2D _rb;

    // Animator 파라미터 해시 캐싱 (GC 방지)
    static readonly int H_IsWalking  = Animator.StringToHash("isWalking");
    static readonly int H_IsRunning  = Animator.StringToHash("isRunning");
    static readonly int H_IsDucking  = Animator.StringToHash("isDucking");
    static readonly int H_IsFalling  = Animator.StringToHash("isFalling");
    static readonly int H_Jump       = Animator.StringToHash("Jump");
    static readonly int H_IsHighJump = Animator.StringToHash("isHighJump");
    static readonly int H_Land       = Animator.StringToHash("Land");
    static readonly int H_Turn       = Animator.StringToHash("Turn");

    // ── 나중에 추가할 파라미터 (구현 시 해시 추가) ──────────────────────────
    // H_IsHurt, H_IsDead, H_IsFever, H_Eat, H_Steal, H_Fight

    bool _wasGrounded;
    bool _wasAboutToLand;
    bool _landTriggeredThisFall;   // 한 번의 낙하에서 Land를 중복 발동하지 않도록

    void Awake()
    {
        _anim = GetComponent<Animator>();
        _rb   = GetComponent<Rigidbody2D>();
    }

    // ── 상태 업데이트 (PlayerController.Update에서 매 프레임 호출) ────────────

    public void UpdateState(float moveInput, bool isGrounded, bool aboutToLand,
                            bool isDucking, bool isDashing, bool isOnLadder)
    {
        bool isMoving = Mathf.Abs(moveInput) > 0.01f;
        _anim.SetBool(H_IsWalking, isMoving && !isDashing && isGrounded);
        _anim.SetBool(H_IsRunning, isDashing);
        _anim.SetBool(H_IsDucking, isDucking);

        // 낙하 감지
        bool isFalling = !isGrounded && _rb != null && _rb.linearVelocity.y < -0.5f;
        _anim.SetBool(H_IsFalling, isFalling);

        // ── Land 모션 발동 ──────────────────────────────────────────────────
        // 도약 직후 공중에 뜨는 순간 플래그 리셋
        if (!isGrounded && _wasGrounded)
            _landTriggeredThisFall = false;

        // _wasAboutToLand는 "실제로 낙하 중일 때 aboutToLand였는가"만 기록
        bool fallingAboutToLand = aboutToLand && isFalling;

        if (!_landTriggeredThisFall)
        {
            // 우선: isFalling 중이고 지면과 preLandDistance 이내일 때 선발동
            if (fallingAboutToLand && !_wasAboutToLand)
            {
                _anim.SetBool(H_IsFalling, false);
                _anim.SetTrigger(H_Land);
                _landTriggeredThisFall = true;
            }
            // 폴백: Raycast 실패 시 실제 착지 순간 발동
            else if (isGrounded && !_wasGrounded)
            {
                _anim.SetBool(H_IsFalling, false);
                _anim.SetTrigger(H_Land);
                _landTriggeredThisFall = true;
            }
        }

        _wasAboutToLand = fallingAboutToLand;
        _wasGrounded    = isGrounded;
    }

    // ── 외부 호출 ────────────────────────────────────────────────────────────

    /// <summary>
    /// 점프 시작 시 호출. isHighJump=true면 Cat_jump_2 포즈, false면 Cat_jump_1 포즈.
    /// </summary>
    public void TriggerJump(bool isHighJump)
    {
        _anim.SetBool(H_IsHighJump, isHighJump);
        _anim.SetTrigger(H_Jump);
    }

    /// <summary>스페이스바를 뗄 때 호출 → 낮은 점프 포즈로 전환</summary>
    public void SetHighJump(bool value) => _anim.SetBool(H_IsHighJump, value);

    /// <summary>대시 시 방향 전환 트리거</summary>
    public void TriggerTurn() => _anim.SetTrigger(H_Turn);
}
