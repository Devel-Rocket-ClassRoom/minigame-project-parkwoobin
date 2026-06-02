using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// PlayerController의 전투·액션 partial.
// - HP 관리(Heal/TakeDamage/Die)
// - 공격 HitBox 생성과 활성화 코루틴
// - 액션 트리거 진입점(Eat/Sleep/Steal/Turn/Hurt/Hungry 토글 등)

public partial class PlayerController
{
    // ── HP / 사망 ────────────────────────────────────────────────────────────

    public void Heal(int amount)
    {
        if (_isDead) return;
        _hp = Mathf.Min(maxHp, _hp + amount);
    }

    public void SetHp(int hp, int newMaxHp)
    {
        maxHp = newMaxHp;
        _hp = Mathf.Clamp(hp, 0, maxHp);
    }

    public bool TakeDamage(int amount, float attackerX = 0f)
    {
        if (_isDead || _invincibleTimer > 0f) return false;
        _hp = Mathf.Max(0, _hp - amount);
        _invincibleTimer = invincibleDuration;
        Debug.Log($"[Player] HP: {_hp}/{maxHp}");
        PlaySfxHit();
        _anim?.SetHurt(true);
        if (_hp <= 0) { Die(); return true; }
        _isHurt = true;
        _hurtTimer = hurtDuration;
        float dir = transform.position.x >= attackerX ? 1f : -1f;
        transform.position += new Vector3(dir * (3f / 32f), 0f, 0f);
        return true;
    }

    /// <summary>게임 오버 처리 — 이동·입력을 즉시 차단하고 속도를 0으로 만듦</summary>
    public void Die()
    {
        if (_isDead) return;
        _isDead = true;
        _rb.linearVelocity = Vector2.zero;
        _rb.gravityScale = _defaultGravityScale;
        DisableAttackHitBox();
        _anim?.SetDead(true);
        GameManager.Instance?.GameOver();
    }

    // ── 공격 HitBox ─────────────────────────────────────────────────────────

    /// <summary>Inspector에 연결된 AttackHitBox가 없으면 플레이어 앞에 자동 생성</summary>
    GameObject BuildAttackHitBox()
    {
        var go = new GameObject("Attack HitBox");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0.4f, 0f, 0f);
        go.layer = gameObject.layer;
        // Kinematic RB 필수: 자체 RB 없으면 OnTriggerEnter2D가 부모(Player)에만 전달됨
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.simulated = true;
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.5f, 0.8f);
        go.AddComponent<AttackHitBox>();
        return go;
    }

    public void TriggerAttack()
    {
        if (_isHiding || _isDead) return;
        if (!CanStartAction()) return;
        if (_fightCooldown > 0f) return;
        _anim?.TriggerFight();
        _fightCooldown = actionResetTime;
        PlaySfxAttack();
        // HitBox는 Animation Event(OnAttackHitFrame)에서 활성화
    }

    /// <summary>Animation Event: 공격 클립의 마지막 2프레임 시작 시점에서 호출</summary>
    public void OnAttackHitFrame()
    {
        if (_isDead) return;
        EnableAttackHitBox();
    }

    public void EnableAttackHitBox()
    {
        if (_attackHitBox == null) return;
        if (_attackCoroutine != null) StopCoroutine(_attackCoroutine);
        _attackHitBox.SetActive(true);
        _attackCoroutine = StartCoroutine(AttackActiveRoutine());
    }

    public void DisableAttackHitBox()
    {
        if (_attackCoroutine != null) { StopCoroutine(_attackCoroutine); _attackCoroutine = null; }
        _attackHitBox?.SetActive(false);
    }

    IEnumerator AttackActiveRoutine()
    {
        var col = _attackHitBox.GetComponent<BoxCollider2D>();
        var hitConfig = _attackHitBox.GetComponent<AttackHitBox>();
        // HitBox 컴포넌트에 Damage가 명시되어 있으면 그것을 우선, 아니면 PlayerController.attackPower 사용
        int dmg = (hitConfig != null && hitConfig.Damage > 0) ? hitConfig.Damage : attackPower;
        var hitEnemies = new HashSet<EnemyBase>();
        float elapsed = 0f;

        while (elapsed < _attackActiveDuration)
        {
            if (col != null)
            {
                Vector2 center = col.bounds.center;
                Vector2 size = col.bounds.size;
                var hits = Physics2D.OverlapBoxAll(center, size, 0f);
                foreach (var h in hits)
                {
                    if (h.gameObject == gameObject) continue;
                    var enemy = h.GetComponentInParent<EnemyBase>();
                    if (enemy != null && hitEnemies.Add(enemy))
                    {
                        Debug.Log($"[PlayerController] 적 적중: {enemy.name}, damage={dmg}");
                        enemy.TakeDamage(dmg, transform.position.x);
                    }
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        _attackHitBox.SetActive(false);
        _attackCoroutine = null;
    }

    // ── 액션 트리거 (외부/입력 진입점) ───────────────────────────────────────

    bool CanStartAction()
    {
        if (_isDead) return false;
        if (_anim != null && _anim.IsActionPlaying()) return false;
        return true;
    }

    public void ToggleHungry()
    {
        if (_isDead) return;
        _isHungry = !_isHungry;
        _anim?.SetHungry(_isHungry);
    }

    public void TriggerSteal()
    {
        if (!CanStartAction()) return;
        if (!_isGrounded) return;
        _anim?.TriggerSteal();
    }

    public void TriggerHurtAnimation()
    {
        if (_isDead) return;
        if (_hurtAnimTimer > 0f) return;
        _anim?.SetHurt(true);
        _hurtAnimTimer = actionResetTime;
    }

    public void TriggerEat()
    {
        if (!CanStartAction()) return;
        _anim?.TriggerEat();
        PlaySfxEat();
    }

    public void TriggerSleep()
    {
        if (!CanStartAction()) return;
        if (!_isGrounded) return;
        _anim?.TriggerSleep();
    }

    public bool TriggerTurn()
    {
        if (_isDead) return false;
        if (!_isGrounded && !IsAscending) return false;
        _anim?.TriggerTurn();
        _invincibleTimer = Mathf.Max(_invincibleTimer, turnDuration);
        PlaySfxTurn();
        if (_turnCoroutine != null) StopCoroutine(_turnCoroutine);
        _turnCoroutine = StartCoroutine(TurnPassthroughRoutine());
        return true;
    }

    IEnumerator TurnPassthroughRoutine()
    {
        int playerLayer = gameObject.layer;
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);

        yield return new WaitForSeconds(turnDuration);

        if (enemyLayer >= 0)
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
        _turnCoroutine = null;
    }
}
