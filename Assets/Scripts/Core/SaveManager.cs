using UnityEngine;

/// <summary>
/// 게임 저장/불러오기 시스템 (PlayerPrefs + JSON 기반)
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const string SAVE_KEY = "CatGame_SaveData";

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>저장 데이터 존재 여부 확인</summary>
    public bool HasSaveData()
    {
        return PlayerPrefs.HasKey(SAVE_KEY);
    }

    /// <summary>게임 저장</summary>
    public void SaveGame(SaveData data)
    {
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
        Debug.Log("[SaveManager] 게임 저장 완료");
    }

    /// <summary>게임 불러오기 — 없으면 null 반환</summary>
    public SaveData LoadGame()
    {
        if (!HasSaveData()) return null;
        string json = PlayerPrefs.GetString(SAVE_KEY);
        return JsonUtility.FromJson<SaveData>(json);
    }

    /// <summary>저장 데이터 삭제</summary>
    public void DeleteSave()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.Save();
        Debug.Log("[SaveManager] 저장 데이터 삭제 완료");
    }
}

[System.Serializable]
public class SaveData
{
    public int stage;
    public int coins;
    public float hp;

    public SaveData()
    {
        stage = 1;
        coins = 0;
        hp = 100f;
    }
}
