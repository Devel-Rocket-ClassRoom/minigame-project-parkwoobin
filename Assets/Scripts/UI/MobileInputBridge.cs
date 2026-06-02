using UnityEngine;

/// <summary>
/// JoystickValue ScriptableObject를 읽어서 PlayerController에 입력을 전달합니다.
/// Player GameObject에 붙이세요. Inspector에서 JValue.asset을 연결하세요.
/// </summary>
public class MobileInputBridge : MonoBehaviour
{
    [Tooltip("JValue.asset을 연결하세요")]
    public JoystickValue joystickValue;

    [Tooltip("Dash/Turn 쿨다운 UI. 없으면 UI 없이 쿨다운만 동작")]
    [SerializeField] SkillCoolTime skillCoolTime;

    PlayerController _player;
    bool _prevJumping;
    bool _prevAttacking;
    bool _prevMoveActive;


    void Start()
    {
        _player = GetComponent<PlayerController>();
        if (_player == null)
            _player = FindFirstObjectByType<PlayerController>();
        if (skillCoolTime == null)
            skillCoolTime = FindFirstObjectByType<SkillCoolTime>();
    }

    void Update()
    {
        if (_player == null || joystickValue == null) return;

        // ── 조이스틱 이동 ─────────────────────────────────────
        // 조이스틱이 실제로 움직일 때만 전달 (0일 때 계속 보내면 키보드 입력을 덮어씀)
        bool moveActive = Mathf.Abs(joystickValue.joyTouch.x) > 0.01f;
        if (moveActive || _prevMoveActive)
            _player.GamepadSetMove(joystickValue.joyTouch.x);
        _prevMoveActive = moveActive;
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
    }

    public void TryDash() => TrySkill(0, () => _player.GamepadDashPress());
    public void TryTurn() => TrySkill(1, () => _player.TriggerTurn());

    void TrySkill(int index, System.Func<bool> action)
    {
        if (_player == null) return;
        if (skillCoolTime != null && skillCoolTime.IsOnCooldown(index)) return;
        if (action())
            skillCoolTime?.HideSkillSetting(index);
    }
}
