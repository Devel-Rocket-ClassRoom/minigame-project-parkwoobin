using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Collider2D))]
public class BoxController : MonoBehaviour
{
    [SerializeField] string openStateName = "Box_Open";

    Animator _animator;
    bool _isPlayerInRange;
    bool _isOpened;

    void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (!_isPlayerInRange || _isOpened) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.fKey.wasPressedThisFrame) OpenBox();
    }

    void OpenBox()
    {
        _isOpened = true;
        _animator.Play(openStateName, 0, 0f);
        Debug.Log("[Box] opened");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _isPlayerInRange = true;
        Debug.Log("[Box] player in range");
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _isPlayerInRange = false;
        Debug.Log("[Box] player left");
    }
}
