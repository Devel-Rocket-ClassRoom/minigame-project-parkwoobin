using UnityEngine;
using UnityEngine.Events;

public class HungerSystem : MonoBehaviour
{
    [SerializeField] float maxHunger = 100f;
    [SerializeField] float depletionRate = 2f;   // per second

    public static UnityAction<float, float> OnHungerChanged;  // current, max

    float _hunger;

    public float Hunger => _hunger;
    public float MaxHunger => maxHunger;

    void Start()
    {
        _hunger = maxHunger;
    }

    void Update()
    {
        if (!GameManager.Instance.IsPlaying) return;

        float prev = _hunger;
        _hunger = Mathf.Max(0f, _hunger - depletionRate * Time.deltaTime);
        if (!Mathf.Approximately(prev, _hunger))
            OnHungerChanged?.Invoke(_hunger, maxHunger);
    }

    public void Eat(float amount)
    {
        _hunger = Mathf.Min(maxHunger, _hunger + amount);
        OnHungerChanged?.Invoke(_hunger, maxHunger);
    }
}
