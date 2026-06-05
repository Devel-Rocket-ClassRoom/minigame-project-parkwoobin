using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public enum HungerAction
{
    Move,
    Jump,
    Skill,
    Attack
}

public class HungerSystem : MonoBehaviour
{
    [SerializeField] float maxHunger = 100f;

    [Header("Action Depletion")]
    [SerializeField, Range(0f, 1f)] float moveDepletionChance = 0.8f;
    [SerializeField] Vector2 moveDepletionRange = new Vector2(0.2f, 0.45f);
    [SerializeField, Range(0f, 1f)] float jumpDepletionChance = 1f;
    [SerializeField] Vector2 jumpDepletionRange = new Vector2(0.8f, 1.5f);
    [SerializeField, Range(0f, 1f)] float skillDepletionChance = 1f;
    [SerializeField] Vector2 skillDepletionRange = new Vector2(1.2f, 2.2f);
    [SerializeField, Range(0f, 1f)] float attackDepletionChance = 1f;
    [SerializeField] Vector2 attackDepletionRange = new Vector2(0.9f, 1.7f);

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

    public void TryDepleteForAction(HungerAction action)
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
        if (_hunger <= 0f) return;

        float chance;
        Vector2 range;
        switch (action)
        {
            case HungerAction.Move:
                chance = moveDepletionChance;
                range = moveDepletionRange;
                break;
            case HungerAction.Jump:
                chance = jumpDepletionChance;
                range = jumpDepletionRange;
                break;
            case HungerAction.Skill:
                chance = skillDepletionChance;
                range = skillDepletionRange;
                break;
            default:
                chance = attackDepletionChance;
                range = attackDepletionRange;
                break;
        }

        if (Random.value > chance) return;

        float prev = _hunger;
        float amount = Random.Range(Mathf.Min(range.x, range.y), Mathf.Max(range.x, range.y));
        _hunger = Mathf.Max(0f, _hunger - amount);
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
