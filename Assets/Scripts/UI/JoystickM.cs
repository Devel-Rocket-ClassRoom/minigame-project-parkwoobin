using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class JoystickM : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    RectTransform rect;
    Vector2 touch = Vector2.zero;
    public RectTransform handle;

    float widthHalf;
    public JoystickValue value;
    private float offset = 0.15f; // 중심에서 이 범위 안은 (0,0)으로 처리

    public void Start()
    {
        rect = GetComponent<RectTransform>();
    }
    public void OnDrag(PointerEventData eventData)
    {
        widthHalf = rect.rect.width * 0.5f;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rect, eventData.position, eventData.pressEventCamera, out localPoint);
        touch = (localPoint - rect.rect.center) / widthHalf;
        if (touch.magnitude < offset)
        {
            touch = Vector2.zero;
            value.joyTouch = Vector2.zero;
        }
        else
        {
            // 핸들 시각 위치는 원 안에 유지
            Vector2 handleTouch = touch.magnitude > 1f ? touch.normalized : touch;
            handle.anchoredPosition = handleTouch * widthHalf;

            // 입력 값은 x, y 각각 독립 클램프 → 대각선 입력 시 수평 속도 손실 없음
            value.joyTouch = new Vector2(
                Mathf.Clamp(touch.x, -1f, 1f),
                Mathf.Clamp(touch.y, -1f, 1f)
            );
        }
        return;
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);

    }

    public void OnPointerUp(PointerEventData eventData)
    {
        value.joyTouch = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
    }

}
