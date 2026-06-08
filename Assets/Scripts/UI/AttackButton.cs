using UnityEngine;
using UnityEngine.EventSystems;

public class AttackButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public JoystickValue value;

    [Tooltip("기본 공격 아이콘")]
    [SerializeField] GameObject AttackIcon;
    [Tooltip("박스 열기 아이콘 (열쇠)")]
    [SerializeField] GameObject KeyIcon;

    void Update()
    {
        bool nearBox = BoxController.Current != null;
        if (AttackIcon != null) AttackIcon.SetActive(!nearBox);
        if (KeyIcon != null) KeyIcon.SetActive(nearBox);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (BoxController.Current != null)
            BoxController.Current.TryOpenBox();
        else
            value.isAttacking = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        value.isAttacking = false;
    }
}
