using UnityEngine;

public enum UpgradeType
{
    Speed,        // 스피드
    DashSpeed,    // 대시 빠르게
    JumpHeight,   // 점프 높게
    HPup,        // 현재 HP 회복
    Eating,    // 현재 배고픔 회복
    DashCooldown, // 대시 쿨타임 감소
    TurnCooldown, // 턴 쿨타임 감소
}

/// <summary>
/// 런 내 업그레이드 레벨을 관리하는 DDOL 싱글톤.
/// 맵마다 3개 무작위 선택 → 코인으로 레벨업.
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    public const int MaxLevel = 5;
    public const int UpgradeCount = 7;

    // ── 업그레이드별 기본 비용 (레벨당 baseCost * (level+1)) ─────────────────
    static readonly int[] BaseCosts = { 10, 10, 10, 10, 10, 15, 15 };

    // ── 레벨당 증가량 ────────────────────────────────────────────────────────
    public static readonly float[] SpeedPerLevel = { 0f, 0.4f, 0.4f, 0.4f, 0.4f, 0.4f };
    public static readonly float[] DashSpeedPerLevel = { 0f, 2f, 2f, 2f, 2f, 2f };
    public static readonly float[] JumpHeightPerLevel = { 0f, 0.2f, 0.2f, 0.2f, 0.2f, 0.2f };
    public const int HpRestoreAmount = 3;
    public const float HungerRestoreAmount = 50f;
    public static readonly float[] DashCoolPerLevel = { 0f, 0.06f, 0.06f, 0.06f, 0.06f, 0.06f };
    public static readonly float[] TurnCoolPerLevel = { 0f, 0.06f, 0.06f, 0.06f, 0.06f, 0.06f };

    int[] _levels = new int[UpgradeCount];

    // 현재 맵에서 보여줄 3개 인덱스
    UpgradeType[] _currentOffers = new UpgradeType[3];
    int _lastOfferedStage = -1;

    // ── 이벤트 ───────────────────────────────────────────────────────────────
    public static System.Action<UpgradeType, int> OnUpgraded; // type, newLevel

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    // ── 공개 API ─────────────────────────────────────────────────────────────

    public int GetLevel(UpgradeType t) => _levels[(int)t];

    public int GetCost(UpgradeType t)
    {
        int lvl = _levels[(int)t];
        if (lvl >= MaxLevel) return 0;
        return BaseCosts[(int)t] * (lvl + 1);
    }

    public bool CanBuy(UpgradeType t)
    {
        if (_levels[(int)t] >= MaxLevel) return false;
        var coinKey = GetCoinKeySystem();
        return coinKey != null && coinKey.Coins >= GetCost(t);
    }

    public bool Buy(UpgradeType t)
    {
        if (_levels[(int)t] >= MaxLevel) return false;

        int cost = GetCost(t);
        var coinKey = GetCoinKeySystem();
        if (coinKey == null || !coinKey.SpendCoins(cost)) return false;

        _levels[(int)t]++;
        OnUpgraded?.Invoke(t, _levels[(int)t]);
        if (t == UpgradeType.HPup)
            UnityEngine.Object.FindFirstObjectByType<PlayerController>()?.Heal(HpRestoreAmount);
        if (t == UpgradeType.Eating)
            UnityEngine.Object.FindFirstObjectByType<HungerSystem>()?.Eat(HungerRestoreAmount);
        ApplyToPlayer();
        return true;
    }

    /// <summary>맵 진입 시 플레이어에 모든 업그레이드 효과 적용.</summary>
    public void ApplyToPlayer()
    {
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (player == null) return;

        // 누적 합계로 적용
        float speedBonus = Sum(SpeedPerLevel, _levels[(int)UpgradeType.Speed]);
        float dashBonus = Sum(DashSpeedPerLevel, _levels[(int)UpgradeType.DashSpeed]);
        float jumpBonus = Sum(JumpHeightPerLevel, _levels[(int)UpgradeType.JumpHeight]);
        float dashCoolBonus = Sum(DashCoolPerLevel, _levels[(int)UpgradeType.DashCooldown]);
        float turnCoolBonus = Sum(TurnCoolPerLevel, _levels[(int)UpgradeType.TurnCooldown]);

        player.SetUpgradeBonuses(speedBonus, dashBonus, jumpBonus, 0, dashCoolBonus, turnCoolBonus);
    }

    /// <summary>현재 스테이지용 3개 업그레이드 제안 반환.</summary>
    public UpgradeType[] GetOffers()
    {
        int stage = GameState.Instance != null ? GameState.Instance.savedStage : 0;
        if (stage != _lastOfferedStage || !AreCurrentOffersValid())
        {
            _lastOfferedStage = stage;
            GenerateOffers();
        }
        return _currentOffers;
    }

    public void InvalidateOffers()
    {
        _lastOfferedStage = -1;
    }

    /// <summary>런 초기화 (새 게임 시 호출).</summary>
    public void ResetAll()
    {
        for (int i = 0; i < UpgradeCount; i++) _levels[i] = 0;
        _lastOfferedStage = -1;
    }

    public int[] GetLevels()
    {
        var copy = new int[UpgradeCount];
        _levels.CopyTo(copy, 0);
        return copy;
    }

    public void SetLevels(int[] levels)
    {
        if (levels == null) return;
        for (int i = 0; i < UpgradeCount && i < levels.Length; i++)
            _levels[i] = Mathf.Clamp(levels[i], 0, MaxLevel);
        _lastOfferedStage = -1;
        ApplyToPlayer();
    }

    // ── 내부 ─────────────────────────────────────────────────────────────────

    void GenerateOffers()
    {
        UpgradeType[] candidates = new UpgradeType[UpgradeCount];
        int count = 0;
        for (int i = 0; i < UpgradeCount; i++)
        {
            var type = (UpgradeType)i;
            if (CanOffer(type))
                candidates[count++] = type;
        }

        for (int i = count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        for (int i = 0; i < 3; i++)
            _currentOffers[i] = candidates[Mathf.Min(i, Mathf.Max(0, count - 1))];
    }

    bool AreCurrentOffersValid()
    {
        for (int i = 0; i < _currentOffers.Length; i++)
        {
            if (!CanOffer(_currentOffers[i]))
                return false;
        }
        return true;
    }

    bool CanOffer(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.DashSpeed:
            case UpgradeType.DashCooldown:
                return SkillUnlockManager.Instance != null &&
                       SkillUnlockManager.Instance.IsSkillActive(SkillType.Dash);
            case UpgradeType.JumpHeight:
                return SkillUnlockManager.Instance != null &&
                       SkillUnlockManager.Instance.IsSkillActive(SkillType.Jump);
            case UpgradeType.TurnCooldown:
                return SkillUnlockManager.Instance != null &&
                       SkillUnlockManager.Instance.IsSkillActive(SkillType.Turn);
            default:
                return true;
        }
    }

    static float Sum(float[] perLevel, int level)
    {
        float total = 0f;
        for (int i = 1; i <= Mathf.Min(level, perLevel.Length - 1); i++)
            total += perLevel[i];
        return total;
    }

    static int SumInt(int[] perLevel, int level)
    {
        int total = 0;
        for (int i = 1; i <= Mathf.Min(level, perLevel.Length - 1); i++)
            total += perLevel[i];
        return total;
    }

    static CoinKeySystem GetCoinKeySystem()
    {
        return CoinKeySystem.Instance != null
            ? CoinKeySystem.Instance
            : UnityEngine.Object.FindFirstObjectByType<CoinKeySystem>();
    }
}
