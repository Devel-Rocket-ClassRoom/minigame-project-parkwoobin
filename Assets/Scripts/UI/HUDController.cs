using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] Slider hpSlider;

    [Header("Hunger")]
    [SerializeField] Slider hungerSlider;

    [Header("Stage")]
    [SerializeField] TextMeshProUGUI stageText;

    [Header("Minimap")]
    [SerializeField] RawImage minimapImage;

    [Header("Coin / Key")]
    [SerializeField] TextMeshProUGUI coinText;
    [SerializeField] TextMeshProUGUI keyText;

    PlayerController _player;

    void OnEnable()
    {
        HungerSystem.OnHungerChanged += OnHungerChanged;
        GameManager.OnStateChanged += OnStateChanged;
        CoinKeySystem.OnCoinChanged += OnCoinChanged;
        CoinKeySystem.OnKeyChanged += OnKeyChanged;
    }

    void OnDisable()
    {
        HungerSystem.OnHungerChanged -= OnHungerChanged;
        GameManager.OnStateChanged -= OnStateChanged;
        CoinKeySystem.OnCoinChanged -= OnCoinChanged;
        CoinKeySystem.OnKeyChanged -= OnKeyChanged;
    }

    void Start()
    {
        _player = FindFirstObjectByType<PlayerController>();
        RefreshHP();
        SetStage(1);
    }

    void Update()
    {
        RefreshHP();
    }

    void RefreshHP()
    {
        if (_player == null || hpSlider == null) return;
        hpSlider.value = (float)_player.Hp / _player.MaxHp;
    }

    void OnHungerChanged(float current, float max)
    {
        if (hungerSlider == null) return;
        hungerSlider.value = current / max;
    }

    void OnCoinChanged(int coins)
    {
        if (coinText != null) coinText.text = coins.ToString();
    }

    void OnKeyChanged(int keys)
    {
        if (keyText != null) keyText.text = $"{keys.ToString()}개";
    }

    void OnStateChanged(GameManager.GameState state) { }

    public void SetStage(int stage)
    {
        if (stageText != null)
            stageText.text = $"Stage {stage}";
    }
}
