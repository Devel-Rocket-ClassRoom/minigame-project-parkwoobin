using UnityEngine;

public partial class PlayerController
{
    [Header("SFX")]
    [SerializeField] AudioClip sfxJump;
    [SerializeField] AudioClip sfxDash;
    [SerializeField] AudioClip sfxAttack;
    [SerializeField] AudioClip sfxTurn;
    [SerializeField] AudioClip sfxHit;
    [SerializeField] AudioClip sfxEat;

    AudioSource _sfxSource;

    void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;
        if (_sfxSource == null)
        {
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
        }
        float vol = AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : 1f;
        _sfxSource.PlayOneShot(clip, vol);
    }

    void PlaySfxJump()   => PlaySfx(sfxJump);
    void PlaySfxDash()   => PlaySfx(sfxDash);
    void PlaySfxAttack() => PlaySfx(sfxAttack);
    void PlaySfxTurn()   => PlaySfx(sfxTurn);
    void PlaySfxHit()    => PlaySfx(sfxHit);
    void PlaySfxEat()    => PlaySfx(sfxEat);
}
