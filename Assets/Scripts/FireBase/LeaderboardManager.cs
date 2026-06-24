using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Firebase.Database;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    private static LeaderboardManager instance;
    public static LeaderboardManager Instance => instance;

    private DatabaseReference leaderboardRef;
    private Query listenerQuery;

    private bool isInitialized;
    private bool isListenerActive;
    public event System.Action<List<LeaderboardEntry>> OnLeaderboardUpdated;

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
            Debug.LogError("[Leaderboard] Firebase 초기화 실패");
            return;
        }

        leaderboardRef = FirebaseInitializer.Instance.Database.RootReference.Child("leaderboard");
        await UniTask.WaitUntil(() => AuthManager.Instance.IsInitialized);
        isInitialized = true;
        Debug.Log("[Leaderboard] 초기화 완료");
    }

    private void OnDestroy()
    {
        StopRealtimeListener();
    }

    public async UniTask UpdateNicknameAsync(string nickname)
    {
        if (!AuthManager.Instance.IsLogedIn || leaderboardRef == null) return;

        string userId = AuthManager.Instance.UserId;
        try
        {
            // 해당 유저의 모든 기록 닉네임 일괄 업데이트
            Query query = leaderboardRef.OrderByChild("userId").EqualTo(userId);
            DataSnapshot snapshot = await query.GetValueAsync();
            if (!snapshot.Exists) return;

            var updates = new Dictionary<string, object>();
            foreach (DataSnapshot child in snapshot.Children)
                updates[$"{child.Key}/nickname"] = nickname;

            await leaderboardRef.UpdateChildrenAsync(updates);
            Debug.Log($"[Leaderboard] 닉네임 일괄 갱신: {nickname} ({snapshot.ChildrenCount}개)");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Leaderboard] 닉네임 갱신 실패: {ex.Message}");
        }
    }

    public async UniTask<(bool success, string error)> SaveToLeaderboardAsync(float clearTime)
    {
        if (!AuthManager.Instance.IsLogedIn)
            return (false, "로그인이 필요합니다.");

        if (leaderboardRef == null)
            return (false, "leaderboardRef 널");

        string userId = AuthManager.Instance.UserId;
        string nickname = ProfileManager.Instance.CachedProfile?.nickname ?? "익명";

        try
        {
            var entryData = new Dictionary<string, object>
            {
                { "userId", userId },
                { "nickname", nickname },
                { "clearTime", clearTime },
                { "timestamp", ServerValue.Timestamp }
            };

            await leaderboardRef.Push().UpdateChildrenAsync(entryData);
            Debug.Log($"[Leaderboard] 저장 성공: {clearTime}s");
            return (true, null);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Leaderboard] 저장 실패: {ex.Message}");
            return (false, ex.Message);
        }
    }

    public async UniTask<List<LeaderboardEntry>> LoadLeaderboardAsync(int limit = 10)
    {
        await UniTask.WaitUntil(() => isInitialized);

        if (leaderboardRef == null)
            return new List<LeaderboardEntry>();

        try
        {
            // 클리어 타임은 낮을수록 좋으므로 오름차순 정렬 후 상위 limit개
            Query query = leaderboardRef.OrderByChild("clearTime").LimitToFirst(limit);
            DataSnapshot snapshot = await query.GetValueAsync();

            List<LeaderboardEntry> leaderboardList = ParseEntries(snapshot);
            Debug.Log("[Leaderboard] 로드 성공");
            return leaderboardList;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Leaderboard] 로드 실패: {ex.Message}");
            return new List<LeaderboardEntry>();
        }
    }

    public List<LeaderboardEntry> ParseEntries(DataSnapshot snapshot)
    {
        var list = new List<LeaderboardEntry>();

        if (snapshot.Exists)
        {
            foreach (DataSnapshot child in snapshot.Children)
                list.Add(LeaderboardEntry.FromJson(child.GetRawJsonValue()));
        }

        // 낮은 클리어 타임이 높은 순위
        list.Sort((a, b) => a.clearTime.CompareTo(b.clearTime));
        return list;
    }

    public void StartRealtimeListener(int limit = 10)
    {
        if (isListenerActive || leaderboardRef == null)
            return;

        listenerQuery = leaderboardRef.OrderByChild("clearTime").LimitToFirst(limit);
        listenerQuery.ValueChanged += OnValueChanged;
        isListenerActive = true;
        Debug.Log("[Leaderboard] 실시간 리스너 시작");
    }

    public void StopRealtimeListener()
    {
        if (isListenerActive && listenerQuery != null)
        {
            listenerQuery.ValueChanged -= OnValueChanged;
            listenerQuery = null;
            isListenerActive = false;
            Debug.Log("[Leaderboard] 실시간 리스너 중지");
        }
    }

    private void OnValueChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError($"[Leaderboard] 리스너 오류: {args.DatabaseError.Message}");
            return;
        }

        List<LeaderboardEntry> leaderboard = ParseEntries(args.Snapshot);
        DispatchUpdateAsync(leaderboard).Forget();
    }

    private async UniTaskVoid DispatchUpdateAsync(List<LeaderboardEntry> leaderboard)
    {
        await UniTask.SwitchToMainThread();
        OnLeaderboardUpdated?.Invoke(leaderboard);
    }
}
