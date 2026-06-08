using UnityEngine;
using UnityEngine.EventSystems;

public class DashButton : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] MobileInputBridge bridge;

    void Start()
    {
        if (bridge == null)
            bridge = FindFirstObjectByType<MobileInputBridge>();
    }

    public void OnPointerDown(PointerEventData eventData) => bridge?.TryDash();
}
