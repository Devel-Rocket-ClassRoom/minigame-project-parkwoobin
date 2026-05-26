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

    public void Start()
    {
        rect = GetComponent<RectTransform>();
        widthHalf = rect.sizeDelta.x * 0.5f;
    }
    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rect, eventData.position, eventData.pressEventCamera, out localPoint);
        touch = localPoint / widthHalf;
        if (touch.magnitude > 1f)
            touch = touch.normalized;
        value.joyTouch = touch;
        handle.anchoredPosition = touch * widthHalf;
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
