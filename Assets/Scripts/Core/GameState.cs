using UnityEngine;

/// <summary>
/// 씬 전환 시 플레이어 스탯(HP·배고픔·코인·열쇠)을 보관하는 DontDestroyOnLoad 싱글톤.
/// ZoneTransition에서 저장 → PlayerSpawner에서 복원.
/// </summary>
public class GameState : MonoBehaviour
{
    private static GameState _instance;

    public static GameState Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("GameState");
                _instance = go.AddComponent<GameState>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // ── 씬 전환 시 저장되는 스탯 ──────────────────────────────────────────────
    public int savedHP;
    public int savedMaxHP;
    public float savedHunger;
    public float savedMaxHunger;
    public int savedCoins;
    public int savedKeys;
    public bool savedFacingLeft;
    public int savedAttack;
    public int savedStage;
    public float savedPositionX;
    public float savedPositionY;
    public bool hasSavedPosition;

    // ── 스킬 잠금 해제 상태 ───────────────────────────────────────────────────
    public bool savedSkillAttack;
    public bool savedSkillJump;
    public bool savedSkillDash;
    public bool savedSkillTurn;
    public bool savedSkillDoubleJump;
    public bool savedSkillWallJump;

    private string _transitionEntryID;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── 진입점 ID ─────────────────────────────────────────────────────────────

    public void SetTransitionEntry(string entryID) => _transitionEntryID = entryID;
    public string GetTransitionEntry() => _transitionEntryID;
    public void ClearTransitionEntry() => _transitionEntryID = null;

    /// <summary>savedMaxHP > 0 이면 유효한 저장 상태. 최초 씬 진입 시엔 false.</summary>
    public bool HasSavedState => savedMaxHP > 0;
}
