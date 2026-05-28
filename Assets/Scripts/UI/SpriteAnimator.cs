using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Image 컴포넌트에 스프라이트 배열을 순서대로 재생해 GIF처럼 보이게 함.
/// Animator / AnimationClip 없이 동작.
/// </summary>
[RequireComponent(typeof(Image))]
public class SpriteAnimator : MonoBehaviour
{
    [SerializeField] Sprite[] frames;
    [SerializeField] float fps = 12f;
    [SerializeField] bool loop = true;

    Image _image;
    float _timer;
    int _index;

    void Awake()
    {
        _image = GetComponent<Image>();
    }

    void OnEnable()
    {
        _index = 0;
        _timer = 0f;
        if (frames != null && frames.Length > 0)
            _image.sprite = frames[0];
    }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;

        _timer += Time.unscaledDeltaTime; // 게임 정지(timeScale=0)에도 재생
        if (_timer < 1f / fps) return;

        _timer -= 1f / fps;
        _index++;

        if (_index >= frames.Length)
        {
            if (!loop) { _index = frames.Length - 1; return; }
            _index = 0;
        }

        _image.sprite = frames[_index];
    }
}
