using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 에디터 키보드 테스트 입력
/// 방향키: 이동 | 아래: 숙이기 | Space: 점프 | A: 공격 | S: 턴 | D: 대시
/// 빌드 배포 시 이 컴포넌트를 비활성화하거나 제거하세요.
/// </summary>
public class KeyboardTestInput : MonoBehaviour
{
    PlayerController _player;
    MobileInputBridge _bridge;

    void Start()
    {
        _player = GetComponent<PlayerController>();
        _bridge = GetComponent<MobileInputBridge>();
        if (_bridge == null)
            _bridge = FindFirstObjectByType<MobileInputBridge>();
        if (_player == null)
            Debug.LogWarning("[KeyboardTestInput] PlayerController를 찾을 수 없습니다.");
    }

    void Update()
    {
        if (_player == null) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        // ── 이동 (좌/우 방향키) ───────────────────────────────────────────────
        float h = 0f;
        if (kb.leftArrowKey.isPressed) h -= 1f;
        if (kb.rightArrowKey.isPressed) h += 1f;
        bool kbMoveUsed = kb.leftArrowKey.isPressed || kb.rightArrowKey.isPressed
                          || kb.leftArrowKey.wasReleasedThisFrame || kb.rightArrowKey.wasReleasedThisFrame;
        if (kbMoveUsed) _player.GamepadSetMove(h);

        // ── 숙이기 (아래 방향키) ─────────────────────────────────────────────
        bool downUsed = kb.downArrowKey.isPressed || kb.downArrowKey.wasReleasedThisFrame;
        if (downUsed)
        {
            bool hiding = kb.downArrowKey.isPressed && _player.IsGrounded && !_player.IsOnLadder;
            _player.GamepadSetHide(hiding);
        }

        // ── 점프 (Space) — 누를 때 / 뗄 때 모두 전달해 가변 점프 유지 ────────
        if (kb.spaceKey.wasPressedThisFrame) _player.GamepadJumpPress();
        if (kb.spaceKey.wasReleasedThisFrame) _player.GamepadJumpRelease();

        // ── 공격 (A) ─────────────────────────────────────────────────────────
        if (kb.aKey.wasPressedThisFrame) _player.TriggerAttack();

        // ── 턴 (S) — 쿨타임 UI 포함 ─────────────────────────────────────────
        if (kb.sKey.wasPressedThisFrame)
        {
            if (_bridge != null) _bridge.TryTurn();
            else _player.TriggerTurn();
        }

        // ── 대시 (D) — 쿨타임 UI 포함 ───────────────────────────────────────
        if (kb.dKey.wasPressedThisFrame)
        {
            if (_bridge != null) _bridge.TryDash();
            else _player.GamepadDashPress();
        }
    }
}
