using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 자막 패널을 화면 하단에 고정한다.
/// CanvasScaler의 레퍼런스 해상도 기준으로 좌표를 계산하므로
/// 어떤 해상도·기기에서도 올바른 위치에 표시된다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SubtitleAnchor : MonoBehaviour
{
    [Tooltip("패널 높이 (레퍼런스 해상도 기준 px)")]
    [SerializeField] float panelHeight = 130f;

    [Tooltip("Safe Area(홈바·노치) 위에서 추가로 띄울 여백 (레퍼런스 해상도 기준 px)")]
    [SerializeField] float bottomPadding = 20f;

    RectTransform _rt;
    CanvasScaler  _scaler;

    void Awake()
    {
        _rt     = GetComponent<RectTransform>();
        _scaler = GetComponentInParent<CanvasScaler>();
        Apply();
    }

    void OnRectTransformDimensionsChange() => Apply();

    void Apply()
    {
        if (_rt == null) return;

        // 앵커: 가로 전체 stretch, 세로 하단 고정
        _rt.anchorMin = new Vector2(0f, 0f);
        _rt.anchorMax = new Vector2(1f, 0f);
        _rt.pivot     = new Vector2(0.5f, 0f);

        // Safe Area 하단을 레퍼런스 해상도 단위로 변환
        float refHeight     = _scaler != null ? _scaler.referenceResolution.y : 1920f;
        float screenHeight  = Screen.height;
        float safeBottomPx  = Screen.safeArea.yMin;                          // 실제 픽셀
        float safeBottomRef = screenHeight > 0
                              ? (safeBottomPx / screenHeight) * refHeight    // 레퍼런스 단위
                              : 0f;

        // 좌우 여백 없음, 높이 고정, 하단에서 safeBottom + padding 만큼 위에 배치
        _rt.offsetMin        = new Vector2(0f, 0f);
        _rt.offsetMax        = new Vector2(0f, 0f);
        _rt.anchoredPosition = new Vector2(0f, safeBottomRef + bottomPadding);
        _rt.sizeDelta        = new Vector2(0f, panelHeight);
    }
}
