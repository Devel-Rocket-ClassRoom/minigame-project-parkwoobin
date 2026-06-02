using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    const string SAVE_KEY_PREFIX = "CatGame_Save_";
    const int SLOT_COUNT = 3;

    /// <summary>현재 진행 중인 슬롯 (0~2). 기본값 0.</summary>
    public int ActiveSlot { get; private set; } = 0;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    // ── 슬롯 조회 ─────────────────────────────────────────────────────────────

    public bool HasSaveData(int slot) => PlayerPrefs.HasKey(Key(slot));

    public SaveData LoadGame(int slot)
    {
        if (!HasSaveData(slot)) return null;
        return JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(Key(slot)));
    }

    public void DeleteSave(int slot)
    {
        PlayerPrefs.DeleteKey(Key(slot));
        PlayerPrefs.Save();
    }

    // ── 저장 ──────────────────────────────────────────────────────────────────

    public void SaveGame(int slot, SaveData data)
    {
        data.savedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        PlayerPrefs.SetString(Key(slot), JsonUtility.ToJson(data));
        PlayerPrefs.Save();
        Debug.Log($"[SaveManager] 슬롯 {slot} 저장 완료 ({data.sceneName})");
    }

    void OnApplicationQuit() => PlayerPrefs.Save();
    void OnApplicationPause(bool pause) { if (pause) PlayerPrefs.Save(); }

    /// <summary>SaveSpot 통과 시 현재 슬롯에 저장.</summary>
    public void AutoSave(PlayerController player = null)
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        var hunger = FindFirstObjectByType<HungerSystem>();
        var coinKey = CoinKeySystem.Instance;
        var gs = GameState.Instance;
        var skill = SkillUnlockManager.Instance;

        var data = new SaveData
        {
            sceneName   = SceneManager.GetActiveScene().name,
            posX        = player != null ? player.transform.position.x : 0f,
            posY        = player != null ? player.transform.position.y : 0f,
            stage       = gs != null ? gs.savedStage : 1,
            coins       = coinKey != null ? coinKey.Coins : 0,
            hp          = player != null ? player.Hp : 0,
            maxHp       = player != null ? player.MaxHp : 0,
            key         = coinKey != null ? coinKey.Keys : 0,
            hunger      = hunger != null ? hunger.Hunger : 0f,
            attack      = player != null ? player.AttackPower : 1,
            skillAttack     = skill != null && skill.attack,
            skillJump       = skill != null && skill.jump,
            skillDash       = skill != null && skill.dash,
            skillTurn       = skill != null && skill.turn,
            skillDoubleJump = skill != null && skill.doubleJump,
            skillWallJump   = skill != null && skill.wallJump,
        };

        SaveGame(ActiveSlot, data);
    }

    // ── 불러오기 → 씬 전환 ────────────────────────────────────────────────────

    /// <summary>슬롯을 활성화하고 저장된 씬으로 이동한다.</summary>
    public void LoadAndApply(int slot)
    {
        var data = LoadGame(slot);
        if (data == null) { Debug.LogWarning($"[SaveManager] 슬롯 {slot} 데이터 없음"); return; }

        ActiveSlot = slot;

        // GameState에 스탯 + 위치 복원 정보 세팅
        var gs = GameState.Instance;
        gs.savedHP         = Mathf.RoundToInt(data.hp);
        gs.savedMaxHP      = data.maxHp > 0 ? data.maxHp : Mathf.RoundToInt(data.hp);
        gs.savedHunger     = data.hunger;
        gs.savedMaxHunger  = 100f;
        gs.savedCoins      = data.coins;
        gs.savedKeys       = data.key;
        gs.savedAttack     = data.attack;
        gs.savedStage      = data.stage;
        gs.savedPositionX  = data.posX;
        gs.savedPositionY  = data.posY;
        gs.hasSavedPosition = true;

        gs.savedSkillAttack     = data.skillAttack;
        gs.savedSkillJump       = data.skillJump;
        gs.savedSkillDash       = data.skillDash;
        gs.savedSkillTurn       = data.skillTurn;
        gs.savedSkillDoubleJump = data.skillDoubleJump;
        gs.savedSkillWallJump   = data.skillWallJump;

        // spawnAtDefault가 true면 저장된 좌표 대신 씬 기본 스폰 포인트 사용
        if (data.spawnAtDefault)
            gs.hasSavedPosition = false;

        SceneTransitionManager.Instance.TransitionTo(data.sceneName, null);
    }

    /// <summary>새 게임 시작 시 슬롯을 지정한다.</summary>
    public void StartNewGame(int slot)
    {
        ActiveSlot = slot;
        DeleteSave(slot);
        if (GameState.Instance != null) GameState.Instance.hasSavedPosition = false;
    }

    // ── 내부 유틸 ─────────────────────────────────────────────────────────────

    static string Key(int slot) => SAVE_KEY_PREFIX + slot;
}

[System.Serializable]
public class SaveData
{
    public string sceneName;
    public float  posX;
    public float  posY;
    public int    stage;
    public int    coins;
    public float  hp;
    public int    maxHp;
    public int    key;
    public float  hunger;
    public int    attack;
    public string savedAt;
    public bool spawnAtDefault;
    public bool skillAttack;
    public bool skillJump;
    public bool skillDash;
    public bool skillTurn;
    public bool skillDoubleJump;
    public bool skillWallJump;
}
