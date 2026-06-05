using System.Collections;
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

    IEnumerator Start()
    {
        _hunger = maxHunger;
        yield return null;  // 한 프레임 대기: 모든 OnEnable 구독 완료 보장
        OnHungerChanged?.Invoke(_hunger, maxHunger);
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

    public void SetHunger(float hunger)
    {
        _hunger = Mathf.Clamp(hunger, 0f, maxHunger);
        OnHungerChanged?.Invoke(_hunger, maxHunger);
    }

    float _hungerBonus;
    /// <summary>UpgradeManager가 최대 배고픔 보너스를 적용한다.</summary>
    public void AddMaxHungerBonus(float bonus)
    {
        maxHunger = maxHunger - _hungerBonus + bonus;
        _hungerBonus = bonus;
        _hunger = Mathf.Min(_hunger, maxHunger);
        OnHungerChanged?.Invoke(_hunger, maxHunger);
    }
}
