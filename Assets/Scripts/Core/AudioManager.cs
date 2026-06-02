using UnityEngine;

/// <summary>
/// BGM / SFX 볼륨을 관리하는 싱글톤.
/// SettingPanelController에서 슬라이더 값을 받아 AudioSource 볼륨에 반영.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] AudioSource bgmSource;
    [SerializeField] AudioSource sfxSource;

    const string KEY_BGM = "BGMVolume";
    const string KEY_SFX = "SFXVolume";

    public float BgmVolume => bgmSource != null ? bgmSource.volume : 1f;
    public float SfxVolume => sfxSource != null ? sfxSource.volume : 1f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // 저장된 볼륨 불러오기
        SetBgm(PlayerPrefs.GetFloat(KEY_BGM, 1f));
        SetSfx(PlayerPrefs.GetFloat(KEY_SFX, 1f));
    }

    public void SetBgm(float volume)
    {
        volume = Mathf.Clamp01(volume);
        if (bgmSource != null) bgmSource.volume = volume;
        PlayerPrefs.SetFloat(KEY_BGM, volume);
    }

    public void PlayBgm(AudioClip clip)
    {
        if (clip == null || bgmSource == null) return;
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBgm()
    {
        bgmSource?.Stop();
    }

    public void SetSfx(float volume)
    {
        volume = Mathf.Clamp01(volume);
        if (sfxSource != null) sfxSource.volume = volume;
        PlayerPrefs.SetFloat(KEY_SFX, volume);
    }

    public void PlaySfx(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }
}
