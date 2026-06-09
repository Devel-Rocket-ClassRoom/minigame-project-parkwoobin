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

    /// <summary>SaveSpot 통과 시 현재 슬롯에 저장. 저장 위치는 SaveSpot 좌표로 고정.</summary>
    public void AutoSaveAtPosition(PlayerController player, Vector2 savePos)
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        var hunger = FindFirstObjectByType<HungerSystem>();
        var coinKey = CoinKeySystem.Instance;
        var gs = GameState.Instance;
        var skill = SkillUnlockManager.Instance;

        var data = new SaveData
        {
            sceneName   = SceneManager.GetActiveScene().name,
            posX        = savePos.x,
            posY        = savePos.y,
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
            upgradeLevels   = UpgradeManager.Instance?.GetLevels(),
        };

        SaveGame(ActiveSlot, data);
    }

    /// <summary>위치 지정 없이 저장 (플레이어 현재 위치 사용). 하위 호환용.</summary>
    public void AutoSave(PlayerController player = null)
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        Vector2 pos = player != null ? (Vector2)player.transform.position : Vector2.zero;
        AutoSaveAtPosition(player, pos);
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

        UpgradeManager.Instance?.SetLevels(data.upgradeLevels);

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

    /// <summary>
    /// 엔딩 도달 시 클리어 상태로 저장.
    /// Ending 씬에는 PlayerController가 없으므로 GameState에서 스탯을 읽는다.
    /// 불러오기하면 Prologue 씬 기본 스폰 포인트에서 시작된다.
    /// </summary>
    public void SaveCleared()
    {
        var gs = GameState.Instance;
        var coinKey = CoinKeySystem.Instance;
        var skill = SkillUnlockManager.Instance;

        var data = new SaveData
        {
            sceneName       = "Prologue",
            spawnAtDefault  = true,
            gameClear       = true,
            posX            = 0f,
            posY            = 0f,
            stage           = gs != null ? gs.savedStage : 1,
            coins           = coinKey != null ? coinKey.Coins : (gs != null ? gs.savedCoins : 0),
            hp              = gs != null ? gs.savedHP : 0,
            maxHp           = gs != null ? gs.savedMaxHP : 0,
            key             = coinKey != null ? coinKey.Keys : (gs != null ? gs.savedKeys : 0),
            hunger          = gs != null ? gs.savedMaxHunger : 100f,
            attack          = gs != null ? gs.savedAttack : 1,
            skillAttack     = skill != null && skill.attack,
            skillJump       = skill != null && skill.jump,
            skillDash       = skill != null && skill.dash,
            skillTurn       = skill != null && skill.turn,
            skillDoubleJump = skill != null && skill.doubleJump,
            skillWallJump   = skill != null && skill.wallJump,
            upgradeLevels   = UpgradeManager.Instance?.GetLevels(),
        };

        SaveGame(ActiveSlot, data);
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
    public bool gameClear;
    public bool skillAttack;
    public bool skillJump;
    public bool skillDash;
    public bool skillTurn;
    public bool skillDoubleJump;
    public bool skillWallJump;
    public int[] upgradeLevels;
}
