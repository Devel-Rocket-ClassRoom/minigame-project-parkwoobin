using UnityEngine;

public enum SkillType { None, Attack, Jump, Dash, Turn, DoubleJump, WallJump }

/// <summary>
/// 인스펙터 체크박스로 스킬을 켜고 끔.
/// 버튼 오브젝트는 SetActive로 제어, 더블점프·벽점프는 PlayerController 플래그를 직접 토글.
/// </summary>
public class SkillUnlockManager : MonoBehaviour
{
    public static SkillUnlockManager Instance { get; private set; }

    [Header("스킬 활성화")]
    [SerializeField] bool attack;
    [SerializeField] bool jump;
    [SerializeField] bool dash;
    [SerializeField] bool turn;
    [SerializeField] bool doubleJump;
    [SerializeField] bool wallJump;

    [Header("버튼 오브젝트 (조이스틱 캔버스)")]
    [SerializeField] GameObject attackButton;
    [SerializeField] GameObject jumpButton;
    [SerializeField] GameObject dashButton;
    [SerializeField] GameObject turnButton;

    [SerializeField] PlayerController player;

    void Awake()
    {
        if (Instance != null) { Destroy(this); return; }
        Instance = this;
    }

    void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        ApplyAll();
    }

    public void UnlockSkill(SkillType type)
    {
        switch (type)
        {
            case SkillType.Attack:     attack     = true; break;
            case SkillType.Jump:       jump       = true; break;
            case SkillType.Dash:       dash       = true; break;
            case SkillType.Turn:       turn       = true; break;
            case SkillType.DoubleJump: doubleJump = true; break;
            case SkillType.WallJump:   wallJump   = true; break;
        }
        ApplyAll();
    }

    void ApplyAll()
    {
        if (attackButton != null) attackButton.SetActive(attack);
        if (jumpButton   != null) jumpButton  .SetActive(jump);
        if (dashButton   != null) dashButton  .SetActive(dash);
        if (turnButton   != null) turnButton  .SetActive(turn);

        if (player != null)
        {
            player.SetDoubleJump(doubleJump);
            player.SetWallJump(wallJump);
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying) return;
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        ApplyAll();
    }
#endif
}
