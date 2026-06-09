using UnityEngine;

/// <summary>
/// 씬 진입 시 BGM을 재생한다. Managers 오브젝트 등 씬 루트에 붙인다.
/// </summary>
public class SceneBgm : MonoBehaviour
{
    [SerializeField] AudioClip bgmClip;

    void Start()
    {
        // MapCutsceneManager가 없는 씬에서는 바로 재생
        // MapCutsceneManager가 있으면 인트로 완료 후 PlayMapBgm()을 직접 호출함
        if (bgmClip != null && AudioManager.Instance != null
            && FindFirstObjectByType<MapCutsceneManager>() == null)
        {
            AudioManager.Instance.PlayBgm(bgmClip);
        }
    }

    /// <summary>
    /// MapCutsceneManager에서 인트로 종료 후 호출 — 이 씬의 BGM을 재생한다.
    /// </summary>
    public void PlayMapBgm()
    {
        if (bgmClip != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayBgm(bgmClip);
    }
}
