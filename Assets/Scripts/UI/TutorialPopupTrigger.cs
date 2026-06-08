using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialPopupTrigger : MonoBehaviour
{
    [SerializeField] SkillType skillToUnlock = SkillType.None;
    [SerializeField] GameObject popupPanel;
    [SerializeField] GameObject dimOverlay;
    [SerializeField] Button closeButton;

    [Header("설명 텍스트")]
    [Tooltip("팝업 패널 안의 설명 TMP_Text. 비워두면 자동으로 팝업 내에서 탐색.")]
    [SerializeField] TMP_Text descriptionText;
    [Tooltip("비워두면 skill 이름으로 자동 생성 (예: tutorial_jump_desc)")]
    [SerializeField] string customDescKey;

    static TutorialPopupTrigger _current;

    bool _triggered;
    bool _waiting;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered) return;
        if (!other.CompareTag("Player")) return;

        if (_current != null) { _waiting = true; return; }

        Activate();
    }
    void Start()
    {

    }

    void Activate()
    {
        _triggered = true;
        _waiting = false;
        _current = this;

        // 연결 안 된 필드는 다른 트리거에서 자동으로 빌려옴
        if (popupPanel == null || dimOverlay == null || closeButton == null)
        {
            foreach (var t in FindObjectsByType<TutorialPopupTrigger>(FindObjectsSortMode.None))
            {
                if (t == this) continue;
                if (popupPanel == null && t.popupPanel != null) popupPanel = t.popupPanel;
                if (dimOverlay == null && t.dimOverlay != null) dimOverlay = t.dimOverlay;
                if (closeButton == null && t.closeButton != null) closeButton = t.closeButton;
                if (popupPanel != null && dimOverlay != null && closeButton != null) break;
            }
        }

        if (popupPanel != null) popupPanel.SetActive(true);
        if (dimOverlay != null) dimOverlay.SetActive(true);
        if (closeButton != null) closeButton.onClick.AddListener(OnClose);

        ShowDescription();
        PlayTipAnimation();

        Time.timeScale = 0f;
        GameManager.Instance?.PauseGame();
    }

    void ShowDescription()
    {
        // descriptionText가 없으면 팝업 패널 내에서 자동 탐색
        // "Tip!" 제목(이름 "Text")은 건드리지 않도록 제외
        if (descriptionText == null && popupPanel != null)
        {
            foreach (var tmp in popupPanel.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp.name == "Text" || tmp.text.Trim() == "Tip!") continue;
                descriptionText = tmp;
                break;
            }
        }

        if (descriptionText == null) return;

        // 키: customDescKey 우선, 없으면 "tutorial_{skillName}_desc"
        string key = !string.IsNullOrEmpty(customDescKey)
            ? customDescKey
            : $"tutorial_{skillToUnlock.ToString().ToLower()}_desc";

        string text = LocalizationManager.Get(key);
        descriptionText.text = string.IsNullOrEmpty(text) ? "" : text;
    }

    void PlayTipAnimation()
    {
        if (popupPanel == null) return;
        // 팝업 패널의 Gif 오브젝트에 있는 SpriteAnimator를 재시작
        var sa = popupPanel.GetComponentInChildren<SpriteAnimator>(true);
        if (sa != null)
        {
            sa.gameObject.SetActive(true);
            sa.enabled = true;
            // OnEnable이 이미 호출됐을 수 있으므로 강제 리셋
            sa.Restart();
        }
    }

    void OnClose()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(OnClose);
        if (popupPanel != null) popupPanel.SetActive(false);
        if (dimOverlay != null) dimOverlay.SetActive(false);

        Time.timeScale = 1f;
        GameManager.Instance?.ResumeGame();

        _current = null;

        SkillUnlockManager.Instance?.UnlockSkill(skillToUnlock);

        // 대기 중인 트리거 다음 것 실행
        foreach (var t in FindObjectsByType<TutorialPopupTrigger>(FindObjectsSortMode.None))
        {
            if (t._waiting && !t._triggered) { t.Activate(); return; }
        }
    }
}
