using UnityEngine;

// PlayerController의 이동·점프·대시·방향 전환 partial.
// 수평 이동 자체는 FixedUpdate에서 직접 처리되며(메인 파일 참고),
// 여기서는 점프 시작·중단·대시 시작·스프라이트 좌우 반전 같은 "트리거성" 로직을 모은다.

public partial class PlayerController
{
    // ── 점프 ─────────────────────────────────────────────────────────────────

    void PressJump()
    {
        if (_isHiding) return;
        if (_isDead) return;
        if (_anim != null && _anim.IsMovementBlocked()) return;  // Eat·Sleep 중 점프 차단
        if (_jumpHeld) return;
        _jumpHeld = true;

        if (_isGrounded) Jump();
        else if (_isOnWall && wallJumpEnabled && !_doubleJumpUsed) WallJump();
        else if (doubleJumpEnabled && _hasDoubleJump && _wallJumpTimer <= 0f) DoubleJump();
    }

    /// <summary>벽에 매달린 상태에서 벽 반대 방향+위로 비스듬히 점프</summary>
    void WallJump()
    {
        // _wallNormalX: 양수=왼쪽 벽(오른쪽으로 튕김), 음수=오른쪽 벽(왼쪽으로 튕김)
        float dirX = _wallNormalX >= 0f ? 1f : -1f;

        float v0 = 2f * maxJumpHeight / maxJumpApexTime;
        _rb.gravityScale = CalculateJumpGravityScale(v0, maxJumpApexTime);
        _rb.linearVelocity = new Vector2(dirX * wallJumpForceX, v0);

        // 바라보는 방향을 튕겨나가는 방향으로 전환
        if (dirX > 0f && !_facingRight) Flip();
        else if (dirX < 0f && _facingRight) Flip();

        // FixedUpdate가 수평 속도를 덮어쓰지 않도록 일정 시간 잠금
        _wallJumpTimer = 0.25f;
        _hasDoubleJump = false;  // 벽 점프 후 더블점프 불가 (착지해야 초기화)

        _anim?.TriggerJump(true);
    }

    void ReleaseJump()
    {
        if (!_jumpHeld) return;
        _jumpHeld = false;
        CutJump();
        _anim?.SetHighJump(false);
    }

    void Jump()
    {
        float v0 = 2f * maxJumpHeight / maxJumpApexTime;
        _rb.gravityScale = CalculateJumpGravityScale(v0, maxJumpApexTime);
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, v0);
        _hasDoubleJump = true;   // 지면 점프 후 더블점프 1회 부여
        _anim?.TriggerJump(true);
    }

    void DoubleJump()
    {
        _hasDoubleJump = false;
        _doubleJumpUsed = true;  // 착지 전까지 벽점프 차단
        float v0 = 1.5f * maxJumpHeight / maxJumpApexTime;
        _rb.gravityScale = CalculateJumpGravityScale(v0, maxJumpApexTime);
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, v0);
        _anim?.TriggerJump(true);
    }

    /// <summary>점프 버튼을 일찍 떼면 상승 속도를 minJumpHeight 기준으로 잘라 가변 점프 구현</summary>
    void CutJump()
    {
        if (_rb.linearVelocity.y <= 0f) return;
        float v0min = 2f * minJumpHeight / minJumpApexTime;
        if (_rb.linearVelocity.y > v0min)
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, v0min);
        _rb.gravityScale = CalculateJumpGravityScale(v0min, minJumpApexTime);
    }

    /// <summary>원하는 정점 시간 안에 도달하도록 중력 스케일을 역산</summary>
    float CalculateJumpGravityScale(float jumpVelocity, float apexTime)
    {
        apexTime = Mathf.Max(0.01f, apexTime);
        return jumpVelocity / (Mathf.Abs(Physics2D.gravity.y) * apexTime);
    }

    // ── 대시 ─────────────────────────────────────────────────────────────────

    void StartDash()
    {
        _isDashing = true;
        _dashTimer = dashDuration;
        float dir = _facingRight ? 1f : -1f;
        _rb.linearVelocity = new Vector2(dir * dashForce, _rb.linearVelocity.y);
    }

    // ── 방향 전환 ────────────────────────────────────────────────────────────

    void UpdateFacing()
    {
        if (_moveInput > 0f && !_facingRight) Flip();
        else if (_moveInput < 0f && _facingRight) Flip();
    }

    void Flip()
    {
        _facingRight = !_facingRight;
        Vector3 s = transform.localScale;
        s.x *= -1f;
        transform.localScale = s;
    }
}
