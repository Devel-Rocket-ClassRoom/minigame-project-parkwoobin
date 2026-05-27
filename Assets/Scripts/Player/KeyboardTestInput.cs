using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 에디터 키보드 테스트 입력
/// 방향키: 이동 | Space: 점프 | A: 공격 | S: 턴 | D: 대시
/// 빌드 배포 시 이 컴포넌트를 비활성화하거나 제거하세요.
/// </summary>
public class KeyboardTestInput : MonoBehaviour
{
    PlayerController _player;

    void Start()
    {
        _player = GetComponent<PlayerController>();
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
        if (kb.leftArrowKey.isPressed)  h -= 1f;
        if (kb.rightArrowKey.isPressed) h += 1f;
        _player.GamepadSetMove(h);

        // ── 점프 (Space) — 누를 때 / 뗄 때 모두 전달해 가변 점프 유지 ────────
        if (kb.spaceKey.wasPressedThisFrame)  _player.GamepadJumpPress();
        if (kb.spaceKey.wasReleasedThisFrame) _player.GamepadJumpRelease();

        // ── 공격 (A) ─────────────────────────────────────────────────────────
        if (kb.aKey.wasPressedThisFrame) _player.TriggerAttack();

        // ── 턴 (S) ───────────────────────────────────────────────────────────
        if (kb.sKey.wasPressedThisFrame) _player.TriggerTurn();

        // ── 대시 (D) ─────────────────────────────────────────────────────────
        if (kb.dKey.wasPressedThisFrame) _player.GamepadDashPress();
    }
}
