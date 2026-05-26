using UnityEngine;
using UnityEngine.InputSystem;

// PlayerController의 입력 관련 partial.
// 모바일 UI(MobileInputBridge)에서 호출하는 Gamepad API만 남김.
// 사다리 수직 이동은 키보드 W/S도 병행 지원.

public partial class PlayerController
{
    // ── 게임패드 가상 입력 상태 ──────────────────────────────────────────────
    float _gamepadVertical;

    // ── 모바일 public API ────────────────────────────────────────────────────
    public void GamepadSetMove(float horizontal)   => _rawMoveInput    = horizontal;
    public void GamepadSetVertical(float vertical) => _gamepadVertical = vertical;
    public void GamepadJumpPress()                 => PressJump();
    public void GamepadJumpRelease()               => ReleaseJump();
    public void GamepadSetHide(bool hiding)        => _isHiding        = hiding;
    public bool GamepadDashPress()
    {
        if (_isHiding || _isDead) return false;
        if (!_isGrounded && _isOnWall) return false;
        if (_isDashing) return false;
        StartDash();
        return true;
    }

    // ── 수직 입력 폴링 (사다리용) ───────────────────────────────────────────
    float ReadVerticalInput()
    {
        float v = _gamepadVertical;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)   v += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v -= 1f;
        }
        return Mathf.Clamp(v, -1f, 1f);
    }
}
