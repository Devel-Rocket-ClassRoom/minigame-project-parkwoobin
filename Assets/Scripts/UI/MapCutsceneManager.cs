using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 맵 시작/종료 컷씬 관리자 (프롤로그와 동일한 페이지·컷 방식).
/// - 인트로: Start()에서 자동 재생 → 완료 시 OnIntroComplete 이벤트 발생
/// - 아웃트로: TriggerOutro() 호출 시 재생 → 완료 후 씬 전환
/// PlayerSpawner는 IntroComplete가 true 될 때까지 박스 낙하를 대기한다.
/// ZoneTransition의 triggerOutro 옵션이 활성화된 존에서 아웃트로가 실행된다.
/// </summary>
public class MapCutsceneManager : MonoBehaviour
{
    [Serializable]
    public class Page
    {
        public Image[] slots;
        public Sprite[] cuts;
        [Tooltip("컷마다 자막 키 — Resources/Localization/strings.json 의 키와 일치해야 함.\n" +
                 "자막 없는 컷은 빈 칸으로 두세요.")]
        public string[] subtitleKeys;
    }

    [Header("인트로 컷씬 (맵 시작)")]
    [SerializeField] Page[] introPages;
    [SerializeField] GameObject introRoot;

    [Header("아웃트로 컷씬 (맵 종료)")]
    [SerializeField] Page[] outroPages;
    [SerializeField] GameObject outroRoot;

    [Header("공통 UI")]
    [SerializeField] Button clickArea;
    [SerializeField] Button skipButton;
    [Tooltip("컷씬 종료 시 숨길 뮤트 버튼 (introRoot/outroRoot 밖에 있는 경우)")]
    [SerializeField] GameObject bgmMuteButton;

    [Header("자막")]
    [SerializeField] TMP_Text subtitleText;
    [Tooltip("자막 텍스트 뒤에 깔리는 패널 (없으면 무시)")]
    [SerializeField] GameObject subtitlePanel;

    [Header("컷씬 중 숨길 UI")]
    [Tooltip("컷씬 재생 중 비활성화할 HUD 오브젝트들 (인트로/아웃트로 공통)")]
    [SerializeField] GameObject[] hideOnCutscene;

    [Header("타이밍")]
    [SerializeField] float cutFadeDuration = 0.2f;
    [SerializeField] float pageFadeDuration = 0.3f;
    [SerializeField] float autoAdvanceDelay = 3f;

    public static MapCutsceneManager Instance { get; private set; }
    public event Action OnIntroComplete;
    public bool IntroComplete { get; private set; }
    public bool HasIntro => introPages != null && introPages.Length > 0;
    public bool HasOutro => outroPages != null && outroPages.Length > 0;

    Page[] _pages;
    int _pageIndex, _cutIndex;
    int _subtitlePageIndex, _subtitleCutIndex; // 현재 표시 중인 컷 위치 (언어 변경 시 갱신용)
    bool _busy, _ended, _lastCutShown;
    Coroutine _autoCoroutine;
    Action _onDone;
    bool _outroActive;

    void Awake()
    {
        Debug.Log($"[MapCutscene] Awake — GO={gameObject.name}, parent={transform.parent?.name ?? "루트"}, HasIntro={HasIntro}");
        Instance = this;
        if (!HasIntro) IntroComplete = true;
        if (introRoot != null) introRoot.SetActive(false);
        if (outroRoot != null) outroRoot.SetActive(false);
        ClearSubtitle();
    }

    void OnEnable() => LanguageManager.OnLanguageChanged += OnLanguageChanged;
    void OnDisable() => LanguageManager.OnLanguageChanged -= OnLanguageChanged;

    // 언어가 바뀌면 현재 컷의 자막을 즉시 갱신.
    // 단, 자막 패널이 이미 표시 중일 때만 — 컷씬 종료 후 언어 변경 시 자막이 다시 뜨는 버그 방지.
    void OnLanguageChanged(LanguageManager.Language _)
    {
        var panel = SubtitlePanelResolved;
        if (panel == null || !panel.activeSelf) return;
        RefreshSubtitle();
    }

    void Start()
    {
        Debug.Log($"[MapCutscene] Start — HasIntro={HasIntro}, introRoot={introRoot}, introPages길이={introPages?.Length ?? 0}");
        if (HasIntro)
        {
            Time.timeScale = 0f;
            Debug.Log("[MapCutscene] 인트로 시작, timeScale=0");
            RunCutscene(introPages, introRoot, OnIntroDone);
        }
    }

    // ── 자막 ────────────────────────────────────────────────────────────────

    void ShowSubtitle(int pageIdx, int cutIdx)
    {
        _subtitlePageIndex = pageIdx;
        _subtitleCutIndex = cutIdx;
        RefreshSubtitle();
    }

    void RefreshSubtitle()
    {
        if (_pages == null || _subtitlePageIndex >= _pages.Length) return;

        var page = _pages[_subtitlePageIndex];
        string key = page.subtitleKeys != null && _subtitleCutIndex < page.subtitleKeys.Length
                     ? page.subtitleKeys[_subtitleCutIndex] : null;

        string text = LocalizationManager.Get(key ?? "");
        bool hasText = !string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(text);

        if (subtitleText != null) subtitleText.text = hasText ? text : "";
        var panel = SubtitlePanelResolved;
        if (panel != null) panel.SetActive(hasText);
    }

    // subtitlePanel이 없으면 subtitleText의 부모 오브젝트를 대신 사용
    GameObject SubtitlePanelResolved =>
        subtitlePanel != null ? subtitlePanel
        : subtitleText != null ? subtitleText.transform.parent?.gameObject
        : null;

    void ClearSubtitle()
    {
        if (subtitleText != null) subtitleText.text = "";
        var panel = SubtitlePanelResolved;
        if (panel != null) panel.SetActive(false);
    }

    // ── HUD / 컷씬UI 표시 ───────────────────────────────────────────────────

    void SetHudVisible(bool visible)
    {
        if (hideOnCutscene == null) return;
        foreach (var go in hideOnCutscene)
            if (go != null) go.SetActive(visible);
    }

    void HideCutsceneUI()
    {
        if (bgmMuteButton != null) bgmMuteButton.SetActive(false);
        ClearSubtitle();
    }

    // ── 인트로 완료 ─────────────────────────────────────────────────────────

    void OnIntroDone()
    {
        if (introRoot != null) introRoot.SetActive(false);
        HideCutsceneUI();
        Time.timeScale = 1f;
        SetHudVisible(true);
        IntroComplete = true;
        OnIntroComplete?.Invoke();
    }

    // ── 아웃트로 트리거 ─────────────────────────────────────────────────────

    public void TriggerOutro(string targetScene, string targetEntryID)
    {
        if (!HasOutro)
        {
            SceneTransitionManager.Instance.TransitionTo(targetScene, targetEntryID);
            return;
        }

        _outroActive = true;
        Time.timeScale = 0f;

        RunCutscene(outroPages, outroRoot, () =>
        {
            _outroActive = false;
            if (outroRoot != null) outroRoot.SetActive(false);
            HideCutsceneUI();
            Time.timeScale = 1f;
            SceneTransitionManager.Instance.TransitionTo(targetScene, targetEntryID);
        });
    }

    // ── 컷씬 실행 ───────────────────────────────────────────────────────────

    void RunCutscene(Page[] pages, GameObject root, Action onDone)
    {
        _pages = pages;
        _onDone = onDone;
        _pageIndex = 0;
        _cutIndex = 0;
        _busy = false;
        _ended = false;
        _lastCutShown = false;

        SetHudVisible(false);
        if (root != null) root.SetActive(true);
        Debug.Log($"[MapCutscene] RunCutscene — root={root?.name}, pages={pages.Length}, root.active={root?.activeSelf}");
        for (int i = 0; i < pages.Length; i++) SetPageActive(i, false);

        clickArea?.onClick.RemoveAllListeners();
        skipButton?.onClick.RemoveAllListeners();
        clickArea?.onClick.AddListener(OnManualClick);
        skipButton?.onClick.AddListener(EndCutscene);

        if (skipButton != null) skipButton.gameObject.SetActive(true);
        if (clickArea != null) clickArea.gameObject.SetActive(true);

        StartPage(0);
        StartCoroutine(ShowFirstCutThenAuto());
    }

    void OnManualClick()
    {
        if (_ended) return;
        if (_autoCoroutine != null) StopCoroutine(_autoCoroutine);
        OnClick();
        if (!_ended) _autoCoroutine = StartCoroutine(AutoAdvance());
    }

    void OnClick()
    {
        if (_busy) return;

        // 마지막 컷이 이미 표시된 상태면 다음 클릭에 종료
        if (_lastCutShown) { EndCutscene(); return; }

        Page page = _pages[_pageIndex];
        int count = Mathf.Min(page.slots.Length, page.cuts.Length);

        if (_cutIndex < count)
        {
            bool isLast = (_pageIndex == _pages.Length - 1) && (_cutIndex == count - 1);
            StartCoroutine(ShowCutAndCheck(page.slots[_cutIndex], page.cuts[_cutIndex],
                                           _pageIndex, _cutIndex, isLast));
            _cutIndex++;
        }
        else
        {
            _pageIndex++;
            if (_pageIndex < _pages.Length)
                StartCoroutine(TurnPage());
        }
    }

    IEnumerator ShowFirstCutThenAuto()
    {
        Debug.Log("[MapCutscene] ShowFirstCutThenAuto 시작");
        Page first = _pages[0];
        if (first.slots.Length > 0 && first.cuts.Length > 0)
        {
            bool isLast = (_pages.Length == 1) && (first.cuts.Length == 1);
            Debug.Log($"[MapCutscene] 첫 컷 표시 — slot={first.slots[0]?.name}, sprite={first.cuts[0]?.name}");
            yield return StartCoroutine(ShowCutAndCheck(first.slots[0], first.cuts[0], 0, 0, isLast));
            _cutIndex = 1;
        }
        if (!_ended)
            _autoCoroutine = StartCoroutine(AutoAdvance());
    }

    IEnumerator AutoAdvance()
    {
        while (!_ended)
        {
            yield return new WaitForSecondsRealtime(autoAdvanceDelay);
            if (!_busy && !_ended) OnClick();
        }
    }

    void StartPage(int idx)
    {
        _pageIndex = idx;
        _cutIndex = 0;
        SetPageActive(idx, true);
        foreach (var slot in _pages[idx].slots)
            if (slot != null) slot.color = Color.clear;
    }

    void SetPageActive(int idx, bool active)
    {
        var slots = _pages[idx].slots;
        if (slots == null || slots.Length == 0) return;
        slots[0].transform.parent.gameObject.SetActive(active);
    }

    IEnumerator ShowCutAndCheck(Image slot, Sprite sprite, int pageIdx, int cutIdx, bool isLast)
    {
        ShowSubtitle(pageIdx, cutIdx);
        yield return StartCoroutine(ShowCut(slot, sprite));
        if (isLast)
        {
            // 마지막 컷 표시 완료 — 바로 종료하지 않고 한 번 더 클릭/자동진행 대기
            _lastCutShown = true;
        }
    }

    IEnumerator ShowCut(Image slot, Sprite sprite)
    {
        _busy = true;
        slot.sprite = sprite;
        slot.color = Color.clear;
        float t = 0f;
        while (t < cutFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            slot.color = new Color(1f, 1f, 1f, Mathf.Clamp01(t / cutFadeDuration));
            yield return null;
        }
        slot.color = Color.white;
        _busy = false;
    }

    IEnumerator TurnPage()
    {
        _busy = true;
        ClearSubtitle();

        Page prev = _pages[_pageIndex - 1];
        float t = 0f;
        while (t < pageFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = 1f - Mathf.Clamp01(t / pageFadeDuration);
            foreach (var slot in prev.slots)
                if (slot != null) slot.color = new Color(1f, 1f, 1f, a);
            yield return null;
        }

        SetPageActive(_pageIndex - 1, false);
        StartPage(_pageIndex);

        Page next = _pages[_pageIndex];
        if (next.slots.Length > 0 && next.cuts.Length > 0)
        {
            bool isLast = (_pageIndex == _pages.Length - 1) && (next.cuts.Length == 1);
            yield return StartCoroutine(ShowCutAndCheck(next.slots[0], next.cuts[0],
                                                        _pageIndex, 0, isLast));
        }
        _cutIndex = 1;
        _busy = false;
    }

    void EndCutscene()
    {
        if (_ended) return;
        _ended = true;
        StopAllCoroutines();
        if (clickArea != null) clickArea.gameObject.SetActive(false);
        if (skipButton != null) skipButton.gameObject.SetActive(false);
        HideCutsceneUI();
        // 인트로 스킵 시에만 HUD 복원 + timeScale 복원 (아웃트로는 씬 전환으로 사라지므로 불필요)
        if (!_outroActive)
        {
            Time.timeScale = 1f;
            SetHudVisible(true);
        }
        _onDone?.Invoke();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
