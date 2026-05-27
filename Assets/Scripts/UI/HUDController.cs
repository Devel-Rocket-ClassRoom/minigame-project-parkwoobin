using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] Slider hpSlider;

    [Header("Hunger")]
    [SerializeField] Slider hungerSlider;

    [Header("Coin / Key")]
    [SerializeField] TextMeshProUGUI coinText;
    [SerializeField] TextMeshProUGUI keyText;

    [Header("Setting")]
    [SerializeField] GameObject settingPanel;
    [SerializeField] Button settingButton;
    [SerializeField] Button settingCloseButton;
    [SerializeField] Image dimOverlay;

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

        if (settingPanel != null) settingPanel.SetActive(false);
        if (settingButton != null) settingButton.onClick.AddListener(OnSettingClick);
        if (settingCloseButton != null) settingCloseButton.onClick.AddListener(CloseSettingPanel);
    }

    public void OnSettingClick()
    {
        bool open = !settingPanel.activeSelf;
        settingPanel?.SetActive(open);
        SetPause(open);
    }

    public void CloseSettingPanel()
    {
        settingPanel?.SetActive(false);
        SetPause(false);
    }

    void SetPause(bool pause)
    {
        Time.timeScale = pause ? 0f : 1f;
        if (dimOverlay != null) dimOverlay.gameObject.SetActive(pause);
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
        if (keyText != null) keyText.text = keys > 0 ? $"{keys.ToString()}개" : "키 없음";
    }

    void OnStateChanged(GameManager.GameState state) { }
}
