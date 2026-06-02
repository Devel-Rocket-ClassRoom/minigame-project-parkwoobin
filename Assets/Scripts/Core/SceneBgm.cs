using UnityEngine;

/// <summary>
/// 씬 진입 시 BGM을 재생한다. Managers 오브젝트 등 씬 루트에 붙인다.
/// </summary>
public class SceneBgm : MonoBehaviour
{
    [SerializeField] AudioClip bgmClip;

    void Start()
    {
        if (bgmClip != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayBgm(bgmClip);
    }
}
