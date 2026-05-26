using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TurnButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public JoystickValue value;
    public void OnPointerDown(PointerEventData eventData)
    {
        value.isTurning = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        value.isTurning = false;
    }
}
