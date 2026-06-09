using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ending 씬 진입 시 게임 클리어 상태를 저장하고 엔딩 UI를 표시한다.
/// LanguageManager.OnLanguageChanged를 구독해 언어 변경 시 텍스트를 갱신한다.
/// </summary>
public class EndingSceneController : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text saveStatusText;
    [SerializeField] Button   mainButton;
    [SerializeField] TMP_Text mainButtonText;

    [Header("씬")]
    [SerializeField] string mainSceneName = "Main";

    bool _saved;

    void Start()
    {
        mainButton?.onClick.AddListener(OnMainClick);
        RefreshTexts();
        StartCoroutine(SaveThenShow());
    }

    void OnEnable()  => LanguageManager.OnLanguageChanged += OnLanguageChanged;
    void OnDisable() => LanguageManager.OnLanguageChanged -= OnLanguageChanged;

    void OnLanguageChanged(LanguageManager.Language _) => RefreshTexts();

    void RefreshTexts()
    {
        if (titleText != null)
            titleText.text = LocalizationManager.Get("ending_title");

        if (saveStatusText != null)
            saveStatusText.text = _saved ? LocalizationManager.Get("ending_saved") : "";

        if (mainButtonText != null)
            mainButtonText.text = LocalizationManager.Get("ending_to_main");
    }

    IEnumerator SaveThenShow()
    {
        yield return new WaitForSeconds(0.5f);

        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveCleared();

        _saved = true;
        if (saveStatusText != null)
            saveStatusText.text = LocalizationManager.Get("ending_saved");
    }

    void OnMainClick()
    {
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.TransitionTo(mainSceneName);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(mainSceneName);
    }
}
