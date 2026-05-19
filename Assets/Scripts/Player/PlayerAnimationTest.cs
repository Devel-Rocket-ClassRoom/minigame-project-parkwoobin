using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 개발용 애니메이션 테스트 입력 컴포넌트.
/// 같은 GameObject에 PlayerAnimationController / PlayerController가 있어야 함.
/// 완성 후 이 컴포넌트를 제거하거나 #if UNITY_EDITOR 로 감싸면 됨.
/// </summary>
[RequireComponent(typeof(PlayerAnimationController))]
public class PlayerAnimationTest : MonoBehaviour
{
    [Header("테스트 설정")]
    [SerializeField] float animResetTime = 0.6f;
    [SerializeField] int attackPower = 1;

    // ── 컴포넌트 참조 ────────────────────────────────────────────────────────
    PlayerAnimationController _anim;
    PlayerController _controller;

    // ── 테스트 상태 ──────────────────────────────────────────────────────────
    bool _testHungry;
    float _throwTimer;
    float _hurtTimer;
    float _fightCooldown;

    // ── 초기화 ───────────────────────────────────────────────────────────────

    void Awake()
    {
        _anim = GetComponent<PlayerAnimationController>();
        _controller = GetComponent<PlayerController>();
    }

    // ── 업데이트 ─────────────────────────────────────────────────────────────

    void Update()
    {
        // 쿨다운 타이머
        if (_throwTimer > 0f) _throwTimer -= Time.deltaTime;
        if (_hurtTimer > 0f) _hurtTimer -= Time.deltaTime;
        if (_fightCooldown > 0f) _fightCooldown -= Time.deltaTime;

        HandleTestInput();
    }

    // ── 테스트 키 입력 ───────────────────────────────────────────────────────

    void HandleTestInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // ── 항상 가능 (액션 중에도 동작) ───────────────────────────────────

        // G — 배고픈 모션 토글
        if (kb.gKey.wasPressedThisFrame)
        {
            _testHungry = !_testHungry;
            _anim.SetHungry(_testHungry);
        }

        // F — 게임 오버 (PlayerController 이동 차단 + GameOver 애니메이션)
        if (kb.fKey.wasPressedThisFrame)
        {
            _controller?.Die();
            _anim.SetDead(true);
        }

        // ── 액션 재생 중에는 아래 입력 전부 차단 ────────────────────────────
        if (_anim.IsActionPlaying()) return;

        // Q — 공격 모션
        if (kb.qKey.wasPressedThisFrame && _fightCooldown <= 0f)
        {
            _anim.TriggerFight();
            _fightCooldown = animResetTime;
            StartCoroutine(AttackHitCheck());
        }

        // S — 스틸 모션 (바닥에서만) — fix #5
        if (kb.sKey.wasPressedThisFrame && (_controller == null || _controller.IsGrounded))
            _anim.TriggerSteal();

        // H — 피격 모션
        if (kb.hKey.wasPressedThisFrame && _hurtTimer <= 0f)
        {
            _anim.SetHurt(true);
            _hurtTimer = animResetTime;
        }

        // T — 던지기 모션 (점프 중에도 가능)
        if (kb.tKey.wasPressedThisFrame && _throwTimer <= 0f)
        {
            _anim.SetThrow(true);
            _throwTimer = animResetTime;
        }

        // E — 음식 먹기 모션
        if (kb.eKey.wasPressedThisFrame)
            _anim.TriggerEat();

        // R — 잠자기 모션 (바닥에서만) — fix #1
        if (kb.rKey.wasPressedThisFrame && (_controller == null || _controller.IsGrounded))
            _anim.TriggerSleep();

        // Shift — Turn/Spin 모션 (바닥 또는 점프 상승 중에만) — fix #9
        bool canTurn = _controller == null
                       || _controller.IsGrounded
                       || _controller.IsAscending;
        if ((kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame) && canTurn)
            _anim.TriggerTurn();
    }

    IEnumerator AttackHitCheck()
    {
        yield return new WaitForSeconds(0.2f);
        float facingDir = transform.localScale.x > 0 ? 1f : -1f;
        var hitCenter = (Vector2)transform.position + Vector2.right * facingDir * 0.6f;
        var hits = Physics2D.OverlapCircleAll(hitCenter, 0.5f);
        foreach (var h in hits)
        {
            if (h.CompareTag("Enemy"))
            {
                var enemy = h.GetComponent<EnemyBase>();
                enemy?.TakeDamage(attackPower, transform.position.x);
            }
        }
    }
}
