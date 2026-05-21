using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 개발용 애니메이션 테스트 입력 컴포넌트.
/// 키 입력을 받아 PlayerController의 트리거 메서드만 호출한다.
/// 조건 검사·쿨다운·토글 상태는 모두 PlayerController가 보유.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerAnimationTest : MonoBehaviour
{
    PlayerController _controller;

    void Awake()
    {
        _controller = GetComponent<PlayerController>();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || _controller == null) return;

        if (kb.gKey.wasPressedThisFrame) _controller.ToggleHungry();
        if (kb.bKey.wasPressedThisFrame) _controller.Die();
        if (kb.qKey.wasPressedThisFrame) _controller.TriggerAttack();
        if (kb.sKey.wasPressedThisFrame) _controller.TriggerSteal();
        if (kb.hKey.wasPressedThisFrame) _controller.TriggerHurtAnimation();
        if (kb.tKey.wasPressedThisFrame) _controller.TriggerThrow();
        if (kb.eKey.wasPressedThisFrame) _controller.TriggerEat();
        if (kb.rKey.wasPressedThisFrame) _controller.TriggerSleep();
        if (kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame)
            _controller.TriggerTurn();
    }
}
