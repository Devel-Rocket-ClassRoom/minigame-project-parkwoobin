using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PrologueManager : MonoBehaviour
{
    [Serializable]
    public class Page
    {
        public Image[] slots;
        public Sprite[] cuts;
    }

    [Header("페이지 배열")]
    [SerializeField] Page[] pages;

    [Header("UI 연결")]
    [SerializeField] Button clickArea;
    [SerializeField] Button skipButton;
    [SerializeField] GameObject startText;
    [SerializeField] Button startTextButton;

    [Header("BGM")]
    [SerializeField] AudioClip bgmClip;

    [Header("씬 전환")]
    [SerializeField] string nextScene = "TutorialMap";

    [Header("페이드")]
    [SerializeField] float cutFadeDuration = 0.2f;
    [SerializeField] float pageFadeDuration = 0.3f;

    [Header("자동 진행")]
    [SerializeField] float autoAdvanceDelay = 3f;

    int _pageIndex;
    int _cutIndex;
    bool _busy;
    bool _ended;
    Coroutine _autoCoroutine;

    void Start()
    {
        clickArea?.onClick.AddListener(OnManualClick);
        skipButton?.onClick.AddListener(EndPrologue);
        startTextButton?.onClick.AddListener(EndPrologue);

        if (startText != null) startText.SetActive(false);

        for (int i = 0; i < pages.Length; i++)
            SetPageActive(i, false);

        StartPage(0);

        // 첫 컷 자동 표시 후 자동 진행 시작
        StartCoroutine(ShowFirstCutThenAuto());
    }

    // 수동 클릭 시 자동 타이머 리셋
    void OnManualClick()
    {
        if (_ended) return;
        if (_autoCoroutine != null) StopCoroutine(_autoCoroutine);
        OnClick();
        if (!_ended)
            _autoCoroutine = StartCoroutine(AutoAdvance());
    }

    void OnClick()
    {
        if (_busy) return;

        Page page = pages[_pageIndex];
        int count = Mathf.Min(page.slots.Length, page.cuts.Length);

        if (_cutIndex < count)
        {
            bool isLastCut = (_pageIndex == pages.Length - 1) && (_cutIndex == count - 1);
            StartCoroutine(ShowCutAndCheck(page.slots[_cutIndex], page.cuts[_cutIndex], isLastCut));
            _cutIndex++;
        }
        else
        {
            _pageIndex++;
            if (_pageIndex < pages.Length)
                StartCoroutine(TurnPage());
        }
    }

    IEnumerator ShowFirstCutThenAuto()
    {
        // 첫 컷 자동 표시
        Page first = pages[0];
        if (first.slots.Length > 0 && first.cuts.Length > 0)
        {
            bool isLastCut = (pages.Length == 1) && (first.cuts.Length == 1);
            yield return StartCoroutine(ShowCutAndCheck(first.slots[0], first.cuts[0], isLastCut));
            _cutIndex = 1;
        }

        if (!_ended)
            _autoCoroutine = StartCoroutine(AutoAdvance());
    }

    IEnumerator AutoAdvance()
    {
        while (!_ended)
        {
            yield return new WaitForSeconds(autoAdvanceDelay);
            if (!_busy && !_ended)
                OnClick();
        }
    }

    void StartPage(int idx)
    {
        _pageIndex = idx;
        _cutIndex = 0;
        SetPageActive(idx, true);

        foreach (var slot in pages[idx].slots)
            if (slot != null) slot.color = Color.clear;
    }

    void SetPageActive(int idx, bool active)
    {
        var slots = pages[idx].slots;
        if (slots == null || slots.Length == 0) return;
        slots[0].transform.parent.gameObject.SetActive(active);
    }

    IEnumerator ShowCutAndCheck(Image slot, Sprite sprite, bool isLast)
    {
        yield return StartCoroutine(ShowCut(slot, sprite));
        if (isLast)
        {
            if (skipButton != null) skipButton.gameObject.SetActive(false);
            if (startText != null) startText.SetActive(true);
            PlayBgm();
            _ended = true;
            if (_autoCoroutine != null) StopCoroutine(_autoCoroutine);
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
            t += Time.deltaTime;
            slot.color = new Color(1f, 1f, 1f, Mathf.Clamp01(t / cutFadeDuration));
            yield return null;
        }
        slot.color = Color.white;
        _busy = false;
    }

    IEnumerator TurnPage()
    {
        _busy = true;

        Page prev = pages[_pageIndex - 1];
        float t = 0f;
        while (t < pageFadeDuration)
        {
            t += Time.deltaTime;
            float a = 1f - Mathf.Clamp01(t / pageFadeDuration);
            foreach (var slot in prev.slots)
                if (slot != null) slot.color = new Color(1f, 1f, 1f, a);
            yield return null;
        }

        SetPageActive(_pageIndex - 1, false);
        StartPage(_pageIndex);

        Page next = pages[_pageIndex];
        if (next.slots.Length > 0 && next.cuts.Length > 0)
        {
            bool isLastCut = (_pageIndex == pages.Length - 1) && (next.cuts.Length == 1);
            yield return StartCoroutine(ShowCutAndCheck(next.slots[0], next.cuts[0], isLastCut));
        }
        _cutIndex = 1;
        _busy = false;
    }

    void PlayBgm()
    {
        if (bgmClip == null) return;

        // AudioManager가 있으면 우선 사용
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBgm(bgmClip);
            return;
        }

        // AudioManager가 없으면 자체 AudioSource 생성
        var src = gameObject.AddComponent<AudioSource>();
        src.clip = bgmClip;
        src.loop = true;
        src.volume = 1f;
        src.Play();
    }

    void EndPrologue()
    {
        _ended = true;
        if (_autoCoroutine != null) StopCoroutine(_autoCoroutine);
        if (clickArea != null) clickArea.interactable = false;
        if (skipButton != null) skipButton.interactable = false;
        SceneTransitionManager.Instance.TransitionTo(nextScene);
    }
}
