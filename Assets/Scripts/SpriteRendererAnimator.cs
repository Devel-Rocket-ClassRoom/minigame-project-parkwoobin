using UnityEngine;

/// <summary>
/// SpriteRenderer용 프레임 애니메이터.
/// (SpriteAnimator는 UI Image 전용 — 게임 오브젝트엔 이 스크립트를 사용)
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteRendererAnimator : MonoBehaviour
{
    [Tooltip("초당 프레임 수")]
    [SerializeField] float fps = 12f;

    [Tooltip("재생할 스프라이트 프레임 배열")]
    [SerializeField] Sprite[] frames;

    [Tooltip("루프 재생 여부")]
    [SerializeField] bool loop = true;

    SpriteRenderer _sr;
    int   _frameIndex;
    float _timer;
    bool  _playing;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        Restart();
    }

    /// <summary>처음 프레임부터 재시작</summary>
    public void Restart()
    {
        _frameIndex = 0;
        _timer      = 0f;
        _playing    = true;

        if (_sr != null && frames != null && frames.Length > 0)
            _sr.sprite = frames[0];
    }

    public void Stop()  => _playing = false;
    public void Play()  { _playing = true; }

    void Update()
    {
        if (!_playing) return;
        if (frames == null || frames.Length <= 1) return;

        _timer += Time.deltaTime;
        float interval = fps > 0f ? 1f / fps : 0.125f;

        while (_timer >= interval)
        {
            _timer -= interval;
            _frameIndex++;

            if (_frameIndex >= frames.Length)
            {
                if (loop) _frameIndex = 0;
                else { _frameIndex = frames.Length - 1; _playing = false; return; }
            }

            _sr.sprite = frames[_frameIndex];
        }
    }
}
