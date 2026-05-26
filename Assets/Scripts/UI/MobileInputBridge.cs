using UnityEngine;

/// <summary>
/// JoystickValue ScriptableObject를 읽어서 PlayerController에 입력을 전달합니다.
/// Player GameObject에 붙이세요. Inspector에서 JValue.asset을 연결하세요.
/// </summary>
public class MobileInputBridge : MonoBehaviour
{
    [Tooltip("JValue.asset을 연결하세요")]
    public JoystickValue joystickValue;

    PlayerController _player;
    bool _prevJumping;
    bool _prevAttacking;
    bool _prevDashing;
    bool _prevTurning;

    void Start()
    {
        _player = GetComponent<PlayerController>();
        if (_player == null)
            _player = FindFirstObjectByType<PlayerController>();
    }

    void Update()
    {
        if (_player == null || joystickValue == null) return;

        // ── 조이스틱 이동 ─────────────────────────────────────
        _player.GamepadSetMove(joystickValue.joyTouch.x);
        _player.GamepadSetVertical(joystickValue.joyTouch.y);

        // ── hide: 조이스틱 아래 + 지면 + 사다리 아님 ─────────
        bool hiding = joystickValue.joyTouch.y < -0.5f
                      && _player.IsGrounded
                      && !_player.IsOnLadder;
        _player.GamepadSetHide(hiding);

        // ── 점프: 누를 때 Press, 뗄 때 Release ──────────────────
        bool jumping = joystickValue.isJumping;
        if (jumping && !_prevJumping)
            _player.GamepadJumpPress();
        else if (!jumping && _prevJumping)
            _player.GamepadJumpRelease();
        _prevJumping = jumping;

        // ── 공격: 누르는 순간 한 번만 트리거 ─────────────────────
        bool attacking = joystickValue.isAttacking;
        if (attacking && !_prevAttacking)
            _player.TriggerAttack();
        _prevAttacking = attacking;

        // ── 대시: 누르는 순간 한 번만 트리거 ─────────────────────
        bool dashing = joystickValue.isDash;
        if (dashing && !_prevDashing)
            _player.GamepadDashPress();
        _prevDashing = dashing;

        // ── 턴: 누르는 순간 한 번만 트리거 ──────────────────────
        bool turning = joystickValue.isTurning;
        if (turning && !_prevTurning)
            _player.TriggerTurn();
        _prevTurning = turning;
    }
}
