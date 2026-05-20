using UnityEngine;
using UnityEngine.Events;

public class CoinKeySystem : MonoBehaviour
{
    public static CoinKeySystem Instance { get; private set; }

    public static UnityAction<int> OnCoinChanged;  // 현재 코인 수
    public static UnityAction<int> OnKeyChanged;   // 현재 열쇠 수

    int _coins;
    int _keys;

    public int Coins => _coins;
    public int Keys => _keys;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void AddCoin(int amount = 1)
    {
        _coins += amount;
        OnCoinChanged?.Invoke(_coins);
    }

    public void AddKey(int amount = 1)
    {
        _keys += amount;
        OnKeyChanged?.Invoke(_keys);
    }

    public bool UseKey()
    {
        if (_keys <= 0) return false;
        _keys--;
        OnKeyChanged?.Invoke(_keys);
        return true;
    }
}
