using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DashButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public JoystickValue value;
    public void OnPointerDown(PointerEventData eventData)
    {
        value.isDash = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        value.isDash = false;
    }
}
