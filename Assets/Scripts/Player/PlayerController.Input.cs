using UnityEngine;
using UnityEngine.InputSystem;

// PlayerController의 입력 관련 partial.
// - Input System(PlayerInput)이 "Send Messages" 모드로 호출하는 OnXxx 메서드들
// - Jump 액션의 started/canceled 이벤트 핸들러
// - Move 액션이 1D Axis(float)라서 수직축이 없어 직접 폴링하는 ReadVerticalInput

public partial class PlayerController
{
    // ── Input System 콜백 (PlayerInput "Send Messages" 모드) ─────────────────

    public void OnMove(InputValue value)
    {
        if (_isDead) return;
        // 실제 키 상태만 보존 — 적용 여부는 Update에서 결정
        _rawMoveInput = value.Get<float>();
    }

    public void OnJump(InputValue value)
    {
        if (_isDead) return;
        if (_isDucking) return;
        if (value.isPressed) PressJump();
        else ReleaseJump();
    }

    public void OnDash(InputValue value)
    {
        if (_isDucking) return;
        if (_isDead) return;
        // 액션 재생 중엔 대시 불가 (캔슬 방지)
        if (_anim != null && _anim.IsActionPlaying()) return;
        // 바닥 또는 점프(상승) 중에만 가능
        if (!_isGrounded && (_rb == null || _rb.linearVelocity.y <= 0f)) return;
        if (value.isPressed && !_isDashing) StartDash();
    }

    public void OnDuck(InputValue value)
    {
        if (!value.isPressed) return;
        if (_isDead) return;
        if (_isDucking)
        {
            _isDucking = false;
        }
        else
        {
            if (!_isGrounded) return;
            _isDucking = true;
            _isDashing = false;
        }
    }

    public void OnAttack(InputValue value)
    {
        if (!value.isPressed) return;
        TriggerAttack();
    }

    // ── Jump 액션의 started/canceled (가변 점프 높이용) ──────────────────────

    void OnJumpStarted(InputAction.CallbackContext ctx)  => PressJump();
    void OnJumpCanceled(InputAction.CallbackContext ctx) => ReleaseJump();

    // ── 수직 입력 폴링 (사다리용) ───────────────────────────────────────────
    // Move 액션이 Axis(float) 타입이라 수직축이 없어서 키보드를 직접 읽음
    float ReadVerticalInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return 0f;
        float v = 0f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed)   v += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v -= 1f;
        return v;
    }
}
