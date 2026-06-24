using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Firebase.Database;
using UnityEngine;
using UnityEngine.Events;

public class ScoreManager : MonoBehaviour
{
    private static ScoreManager instance;
    public static ScoreManager Instance => instance;

    private DatabaseReference scoresRef;

    private float cachedBestTime = float.MaxValue;
    public float CachedBestTime => cachedBestTime;

    // 누적 타이머
    private float accumulatedTime = 0f;
    private float sessionResumeTime = 0f;
    private bool isTiming = false;
    private bool isGameActive = false;

    public float CurrentElapsedTime => isTiming
        ? accumulatedTime + (Time.time - sessionResumeTime)
        : accumulatedTime;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private async UniTaskVoid Start()
    {
        if (!await FirebaseInitializer.Instance.WaitForInitializationAsync())
        {
            Debug.LogError("[Score] 파이어 베이스 초기화 X");
            return;
        }

        await UniTask.WaitUntil(() => AuthManager.Instance.IsInitialized);
        await UniTask.WaitUntil(() => ProfileManager.Instance.IsInitialized);

        scoresRef = FirebaseInitializer.Instance.Database.RootReference.Child("scores");
        Debug.Log("[Score] 초기화 완료");

        AuthManager.Instance.LoginStateChanged += OnLoginStateChanged;

        if (AuthManager.Instance.IsLogedIn)
        {
            await LoadBestTimeAsync();
        }
    }

    private void OnEnable()
    {
        GameManager.OnStateChanged += OnGameStateChanged;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        GameManager.OnStateChanged -= OnGameStateChanged;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (scene.name == "Main")
        {
            // 메인으로 돌아오면 타이머 초기화
            PauseTimer();
            accumulatedTime = 0f;
            isGameActive = false;
        }
        else if (scene.name != "Main")
        {
            // 게임 씬 진입 시 아직 타이머 미시작이면 새로 시작
            if (!isGameActive)
            {
                accumulatedTime = 0f;
                isGameActive = true;
            }
        }
    }

    private void OnGameStateChanged(GameManager.GameState state)
    {
        if (!isGameActive) return;

        if (state == GameManager.GameState.Playing)
        {
            ResumeTimer();
        }
        else if (state == GameManager.GameState.Paused)
        {
            PauseTimer();
        }
    }

    public void ResumeTimer()
    {
        if (!isTiming)
        {
            sessionResumeTime = Time.time;
            isTiming = true;
        }
    }

    public void PauseTimer()
    {
        if (isTiming)
        {
            accumulatedTime += Time.time - sessionResumeTime;
            isTiming = false;
        }
    }

    public async UniTask<float> FinishGameAsync()
    {
        PauseTimer();
        isGameActive = false;
        float clearTime = accumulatedTime;
        accumulatedTime = 0f;
        Debug.Log($"[Score] 클리어 타임: {clearTime:F2}s");
        await SaveClearTimeAsync(clearTime);
        return clearTime;
    }

    private void OnDestroy()
    {
        if (AuthManager.Instance != null)
            AuthManager.Instance.LoginStateChanged -= OnLoginStateChanged;
    }

    private void OnLoginStateChanged(bool isLoggedIn)
    {
        if (isLoggedIn)
            LoadBestTimeAsync().Forget();
        else
            cachedBestTime = float.MaxValue;
    }


    public async UniTask<(bool success, string error)> SaveClearTimeAsync(float clearTime)
    {
        if (!AuthManager.Instance.IsLogedIn)
            return (false, "로그인 필요");

        string userId = AuthManager.Instance.UserId;

        try
        {
            DatabaseReference newHistoryRef = scoresRef.Child(userId).Child("history").Push();
            var data = new Dictionary<string, object>
            {
                { "clearTime", clearTime },
                { "timestamp", ServerValue.Timestamp }
            };
            await newHistoryRef.UpdateChildrenAsync(data);

            if (clearTime < cachedBestTime)
                await UpdateBestTimeAsync(clearTime);

            await LeaderboardManager.Instance.SaveToLeaderboardAsync(clearTime);

            Debug.Log($"[Score] 클리어 타임 저장 성공: {clearTime}s");
            return (true, null);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Score] 클리어 타임 저장 실패: {ex}");
            return (false, ex.Message);
        }
    }

    private async UniTask UpdateBestTimeAsync(float newBestTime)
    {
        string userId = AuthManager.Instance.UserId;

        try
        {
            await scoresRef.Child(userId).Child("besttime").SetValueAsync(newBestTime);
            cachedBestTime = newBestTime;
            Debug.Log($"[Score] 최고 기록 갱신: {newBestTime}s");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Score] 최고 기록 갱신 실패: {ex}");
        }
    }

    public async UniTask<float> LoadBestTimeAsync()
    {
        string userId = AuthManager.Instance.UserId;

        try
        {
            DataSnapshot snapshot = await scoresRef.Child(userId).Child("besttime").GetValueAsync();
            cachedBestTime = snapshot.Exists ? (float)(double)snapshot.Value : float.MaxValue;
            Debug.Log($"[Score] 최고 기록 로드 성공: {cachedBestTime}s");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Score] 최고 기록 로드 실패: {ex}");
            return float.MaxValue;
        }
        return cachedBestTime;
    }

    public async UniTask<List<ScoreData>> LoadHistoryAsync(int limit = 10)
    {
        if (!AuthManager.Instance.IsLogedIn || scoresRef == null)
            return new List<ScoreData>();

        string userId = AuthManager.Instance.UserId;

        try
        {
            Query query = scoresRef.Child(userId).Child("history").OrderByChild("timestamp").LimitToLast(limit);
            DataSnapshot snapshot = await query.GetValueAsync();

            var historyList = new List<ScoreData>();
            if (snapshot.Exists)
            {
                foreach (DataSnapshot child in snapshot.Children)
                    historyList.Add(JsonUtility.FromJson<ScoreData>(child.GetRawJsonValue()));
            }

            return historyList;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Score] 히스토리 로드 실패: {ex}");
            return new List<ScoreData>();
        }
    }
}
