using UnityEngine;

// HitBox 오브젝트에 붙이는 설정 컴포넌트.
// 실제 적 감지는 PlayerController.AttackActiveRoutine이 처리.
public class AttackHitBox : MonoBehaviour
{
    [SerializeField] public int Damage = 1;
}
