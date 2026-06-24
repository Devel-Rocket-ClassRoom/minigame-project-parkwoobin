using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField]
    private Transform leaderboardListParent;

    [SerializeField]
    private GameObject leaderboardEntryPrefab;

    [SerializeField]
    private Button refreshButton;

    [SerializeField]
    private Toggle realtimeToggle;

    [SerializeField]
    private TextMeshProUGUI statusText;

    [SerializeField]
    private Button closeButton;

    [SerializeField]
    private Button rankingButton;

    [SerializeField]
    private GameObject rankingPopup;

    [Header("Settings")]
    [SerializeField]
    private int topCount = 10;

    private bool isRealtimeEnabled = false;

    private void Start()
    {
        refreshButton?.onClick.AddListener(() => LoadAndDisplayLeaderboardAsync().Forget());
        realtimeToggle?.onValueChanged.AddListener(OnRealtimeToggleChanged);
        closeButton?.onClick.AddListener(() => rankingPopup?.SetActive(false));
        rankingButton?.onClick.AddListener(Open);

        LeaderboardManager.Instance.OnLeaderboardUpdated += OnLeaderboardUpdated;
    }

    public void Open()
    {
        rankingPopup?.SetActive(true);
        LoadAndDisplayLeaderboardAsync().Forget();
    }

    private async UniTaskVoid LoadAndDisplayLeaderboardAsync()
    {
        statusText.text = LocalizationManager.Get("FireBase_Syncing");

        List<LeaderboardEntry> leaderboard = await LeaderboardManager.Instance.LoadLeaderboardAsync(topCount);

        DisplayLeaderboard(leaderboard);
        LeaderboardManager.Instance.StartRealtimeListener(topCount);
    }

    private void DisplayLeaderboard(List<LeaderboardEntry> leaderboard)
    {
        foreach (Transform child in leaderboardListParent)
        {
            Destroy(child.gameObject);
        }

        int rank = 1;
        foreach (LeaderboardEntry entry in leaderboard)
        {
            GameObject item = Instantiate(leaderboardEntryPrefab, leaderboardListParent);

            TextMeshProUGUI[] texts = item.GetComponentsInChildren<TextMeshProUGUI>();

            if (texts.Length >= 3)
            {
                texts[0].text = $"{rank}";
                texts[1].text = entry.nickname;
                texts[2].text = TimeUtil.FormatClearTime(entry.clearTime);
            }

            rank++;
        }

        Debug.Log($"[LeaderboardUI] 리더보드 표시 완료: {leaderboard.Count}명");
    }

    private void OnRealtimeToggleChanged(bool isOn)
    {
        if (isOn)
        {
            LeaderboardManager.Instance.StartRealtimeListener(topCount);
            statusText.text = LocalizationManager.Get("FireBase_Syncing");
        }
        else
        {
            LeaderboardManager.Instance.StopRealtimeListener();
            statusText.text = LocalizationManager.Get("FireBase_SyncStopped");
        }
    }

    private void OnLeaderboardUpdated(List<LeaderboardEntry> leaderboard)
    {
        Debug.Log("[LeaderboardUI] 실시간 업데이트 수신");
        DisplayLeaderboard(leaderboard);
        statusText.text = string.Format(LocalizationManager.Get("FireBase_SyncComplete"), leaderboard.Count);
    }


    private void OnDestroy()
    {
        LeaderboardManager.Instance.OnLeaderboardUpdated -= OnLeaderboardUpdated;
        LeaderboardManager.Instance.StopRealtimeListener();
    }
}
