using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메인 메뉴 도움말 패널.
/// 각 팁마다 애니메이션 상태 + 설명 텍스트를 함께 표시한다.
/// </summary>
public class MainMenuTipsPanel : MonoBehaviour
{
    [Serializable]
    public class TipEntry
    {
        [Tooltip("Tip.controller의 State 이름 (Move / Jump / Dash 등)")]
        public string animState;
        [Tooltip("strings.json 키 (설명 텍스트)")]
        public string descKey;
    }

    [Header("UI 연결")]
    [SerializeField] CanvasGroup panelGroup;  // SetActive 대신 alpha로 숨김 → 코루틴 항상 사용 가능
    [SerializeField] GameObject  dimOverlay;   // 패널 뒤에 깔리는 어두운 배경
    [SerializeField] Button     closeButton;
    [SerializeField] Button     prevButton;
    [SerializeField] Button     nextButton;
    [SerializeField] TMP_Text   descText;
    [SerializeField] TMP_Text   pageText;

    [Header("애니메이션")]
    [Tooltip("Tip.controller가 붙어있는 Animator")]
    [SerializeField] Animator   tipAnimator;

    [Header("팁 목록")]
    [SerializeField] TipEntry[] tips = new TipEntry[]
    {
        new TipEntry { animState = "Move",       descKey = "tutorial_move_desc"       },
        new TipEntry { animState = "Jump",        descKey = "tutorial_jump_desc"       },
        new TipEntry { animState = "DoubleJump",  descKey = "tutorial_doublejump_desc" },
        new TipEntry { animState = "Dash",        descKey = "tutorial_dash_desc"       },
        new TipEntry { animState = "WallJump",    descKey = "tutorial_walljump_desc"   },
        new TipEntry { animState = "Turn",        descKey = "tutorial_turn_desc"       },
        new TipEntry { animState = "Attack",      descKey = "tutorial_attack_desc"     },
    };

    int _index;

    void Awake()
    {
        EnsureGroup();
        SetVisible(false);
        closeButton?.onClick.AddListener(Hide);
        prevButton?.onClick.AddListener(Prev);
        nextButton?.onClick.AddListener(Next);

        if (tipAnimator != null)
            tipAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
    }

    // panelGroup이 비어있거나 참조가 깨졌으면 같은 오브젝트에서 확보
    void EnsureGroup()
    {
        if (panelGroup == null) panelGroup = GetComponent<CanvasGroup>();
        if (panelGroup == null) panelGroup = gameObject.AddComponent<CanvasGroup>();
    }

    void SetVisible(bool visible)
    {
        EnsureGroup();
        panelGroup.alpha          = visible ? 1f : 0f;
        panelGroup.interactable   = visible;
        panelGroup.blocksRaycasts = visible;
    }

    void OnEnable()  => LanguageManager.OnLanguageChanged += OnLangChanged;
    void OnDisable() => LanguageManager.OnLanguageChanged -= OnLangChanged;
    void OnLangChanged(LanguageManager.Language _) => RefreshText();

    public void Show()
    {
        if (tips == null || tips.Length == 0) return;
        _index = 0;
        if (dimOverlay != null) dimOverlay.SetActive(true);
        SetVisible(true);
        RefreshText();
        StartCoroutine(PlayAnimNextFrame());
    }

    System.Collections.IEnumerator PlayAnimNextFrame()
    {
        yield return null; // Animator가 완전히 활성화된 다음 프레임에 재생
        PlayAnimation();
    }

    public void Hide()
    {
        SetVisible(false);
        if (dimOverlay != null) dimOverlay.SetActive(false);
    }

    void Prev()
    {
        _index = (_index - 1 + tips.Length) % tips.Length;
        Refresh();
    }

    void Next()
    {
        _index = (_index + 1) % tips.Length;
        Refresh();
    }

    void Refresh()
    {
        RefreshText();
        PlayAnimation();
    }

    void RefreshText()
    {
        if (tips == null || tips.Length == 0) return;

        var entry = tips[_index];
        if (descText != null)
            descText.text = LocalizationManager.Get(entry.descKey);
        if (pageText != null)
            pageText.text = $"{_index + 1} / {tips.Length}";

        bool multi = tips.Length > 1;
        if (prevButton != null) prevButton.gameObject.SetActive(multi);
        if (nextButton != null) nextButton.gameObject.SetActive(multi);
    }

    void PlayAnimation()
    {
        if (tipAnimator == null || tips == null || tips.Length == 0) return;
        string state = tips[_index].animState;
        if (!string.IsNullOrEmpty(state))
            tipAnimator.Play(state, 0, 0f);
    }
}
