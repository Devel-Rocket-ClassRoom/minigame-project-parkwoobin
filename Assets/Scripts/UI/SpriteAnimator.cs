using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI Image 컴포넌트의 스프라이트를 프레임 단위로 바꿔 GIF처럼 재생.
/// Time.timeScale=0 중에도 동작하도록 unscaledDeltaTime 사용.
/// </summary>
[RequireComponent(typeof(Image))]
public class SpriteAnimator : MonoBehaviour
{
    [Tooltip("초당 프레임 수")]
    [SerializeField] float fps = 12f;

    [SerializeField] Sprite[] frames;

    Image _image;
    int _frameIndex;
    float _timer;

    void Awake()
    {
        _image = GetComponent<Image>();

        if (frames == null || frames.Length == 0)
            frames = LoadSpritesFromTexture();

        if (frames == null || frames.Length == 0)
            frames = _image.sprite != null ? new[] { _image.sprite } : null;
    }

    void OnEnable()
    {
        Restart();
    }

    public void Restart()
    {
        _frameIndex = 0;
        _timer = 0f;
        if (_image != null && frames != null && frames.Length > 0)
            _image.sprite = frames[0];
    }

    void Update()
    {
        if (frames == null || frames.Length <= 1) return;

        _timer += Time.unscaledDeltaTime;
        float interval = fps > 0f ? 1f / fps : 0.125f;

        while (_timer >= interval)
        {
            _timer -= interval;
            _frameIndex = (_frameIndex + 1) % frames.Length;
            _image.sprite = frames[_frameIndex];
        }
    }

    Sprite[] LoadSpritesFromTexture()
    {
#if UNITY_EDITOR
        if (_image == null || _image.sprite == null) return null;

        string path = UnityEditor.AssetDatabase.GetAssetPath(_image.sprite.texture);
        if (string.IsNullOrEmpty(path)) return null;

        var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .OrderBy(s => s.name)
            .ToArray();

        return all.Length > 0 ? all : null;
#else
        return null;
#endif
    }
}
