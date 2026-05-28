using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// DontDestroyOnLoad 싱글톤.
/// 씬 간 페이드 아웃 → LoadSceneAsync → 페이드 인 전환을 전담한다.
///
/// [씬 설정]
/// 첫 번째 씬 Hierarchy에 빈 오브젝트(이름 예: "SceneTransitionManager")를 만들고
/// 이 컴포넌트를 붙인다. 하위에 아래 UI Canvas를 구성한다:
///   └ Canvas (Screen Space - Overlay, Sort Order 99)
///       └ FadeImage (Image, 검정, Rect 전체, RaycastTarget OFF)
/// fadeImage 필드에 FadeImage를 연결하면 된다.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    private static SceneTransitionManager _instance;

    /// <summary>
    /// 씬에 오브젝트가 없으면 페이드 없는 최소 인스턴스를 자동 생성한다.
    /// 씬에 직접 배치된 오브젝트가 있으면 그것이 우선이다.
    /// </summary>
    public static SceneTransitionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("SceneTransitionManager(Auto)");
                _instance = go.AddComponent<SceneTransitionManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1f;
    [Header("Fade 설정 (체크 = 해당 페이드 비활성화)")]
    [SerializeField] private bool disableFadeIn  = false;
    [SerializeField] private bool disableFadeOut = false;

    public bool IsTransitioning { get; private set; }

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // fadeImage가 없거나 이 오브젝트의 자식이 아니면 코드로 생성.
        // Inspector에서 씬 오브젝트를 연결하면 씬 언로드 시 참조가 파괴되므로
        // DDOL 자식 Canvas를 항상 직접 만들어 사용한다.
        if (fadeImage == null || !fadeImage.transform.IsChildOf(transform))
            fadeImage = CreateFadeCanvas();

        // FadeIn 활성: 시작 시 검정(불투명) 준비 → Start()에서 FadeIn
        // FadeIn 비활성: 투명 상태로 숨김
        if (!disableFadeIn)
        {
            SetAlpha(1f);
            fadeImage.gameObject.SetActive(true);
        }
        else
        {
            SetAlpha(0f);
            fadeImage.gameObject.SetActive(false);
        }
    }

    Image CreateFadeCanvas()
    {
        var canvasGO = new GameObject("FadeCanvas");
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();

        var imgGO = new GameObject("FadeImage");
        imgGO.transform.SetParent(canvasGO.transform, false);

        // Image를 먼저 추가해야 RectTransform이 자동 생성됨
        var img = imgGO.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;

        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta   = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        return img;
    }

    private void Start()
    {
        // 최초 씬 진입 페이드 인
        StartCoroutine(FadeInCoroutine());
    }

    // ── 외부 호출 ──────────────────────────────────────────────────────────

    /// <summary>
    /// 페이드 아웃 → 씬 로드 → 페이드 인.
    /// ZoneTransition, GameOverManager에서 호출한다.
    /// </summary>
    /// <param name="sceneName">로드할 씬 이름 (Build Settings에 등록 필요)</param>
    /// <param name="entryID">대상 씬에서 찾을 SpawnPoint ID. 비우면 GameState의 기존 스폰 설정(체크포인트 복귀 등)을 따른다.</param>
    public void TransitionTo(string sceneName, string entryID = null)
    {
        if (IsTransitioning) return;

        if (!string.IsNullOrEmpty(entryID) && GameState.Instance != null)
            GameState.Instance.SetTransitionEntry(entryID);

        StartCoroutine(TransitionCoroutine(sceneName));
    }

    // ── 코루틴 ────────────────────────────────────────────────────────────

    private IEnumerator TransitionCoroutine(string sceneName)
    {
        IsTransitioning = true;

        yield return StartCoroutine(FadeOutCoroutine());
        yield return SceneManager.LoadSceneAsync(sceneName);
        yield return StartCoroutine(FadeInCoroutine());

        IsTransitioning = false;
    }

    public IEnumerator FadeInCoroutine()
    {
        if (disableFadeIn || fadeImage == null) yield break;
        fadeImage.gameObject.SetActive(true);
        yield return StartCoroutine(Fade(1f, 0f));
        fadeImage.gameObject.SetActive(false);
    }

    public IEnumerator FadeOutCoroutine()
    {
        if (disableFadeOut || fadeImage == null) yield break;
        fadeImage.gameObject.SetActive(true);
        yield return StartCoroutine(Fade(0f, 1f));
    }

    // ── 내부 유틸 ─────────────────────────────────────────────────────────

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime; // 히트스톱(timeScale=0) 중에도 동작
            SetAlpha(Mathf.Lerp(from, to, elapsed / fadeDuration));
            yield return null;
        }
        SetAlpha(to);
    }

    private void SetAlpha(float a)
    {
        if (fadeImage == null) return;
        Color c = fadeImage.color;
        c.a = a;
        fadeImage.color = c;
    }
}
