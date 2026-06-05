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

    [Header("상점 / 도움말")]
    [SerializeField] Button shopButton;
    [SerializeField] ShopPanel shopPanel;
    [SerializeField] Button helpButton;
    [SerializeField] TipsPanelController tipsPanel;

    PlayerController _player;

    int _keys;  // 언어 변경 시 키 텍스트 재구성용

    void OnEnable()
    {
        HungerSystem.OnHungerChanged += OnHungerChanged;
        GameManager.OnStateChanged += OnStateChanged;
        CoinKeySystem.OnCoinChanged += OnCoinChanged;
        CoinKeySystem.OnKeyChanged += OnKeyChanged;
        LanguageManager.OnLanguageChanged += OnLanguageChanged;
    }

    void OnDisable()
    {
        HungerSystem.OnHungerChanged -= OnHungerChanged;
        GameManager.OnStateChanged -= OnStateChanged;
        CoinKeySystem.OnCoinChanged -= OnCoinChanged;
        CoinKeySystem.OnKeyChanged -= OnKeyChanged;
        LanguageManager.OnLanguageChanged -= OnLanguageChanged;
    }

    void OnLanguageChanged(LanguageManager.Language _) => RefreshKeyText();

    void Start()
    {
        _player = FindFirstObjectByType<PlayerController>();
        RefreshHP();

        if (settingPanel != null) settingPanel.SetActive(false);
        if (settingButton != null) settingButton.onClick.AddListener(OnSettingClick);
        if (settingCloseButton != null) settingCloseButton.onClick.AddListener(CloseSettingPanel);

        shopButton?.onClick.AddListener(() => shopPanel?.Show());
        helpButton?.onClick.AddListener(() => tipsPanel?.Show());

        // 씬 진입 시 업그레이드 효과 적용
        UpgradeManager.Instance?.ApplyToPlayer();
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
        _keys = keys;
        RefreshKeyText();
    }

    void RefreshKeyText()
    {
        if (keyText == null) return;
        keyText.text = _keys > 0
            ? string.Format(LocalizationManager.Get("hud_key_count"), _keys)
            : LocalizationManager.Get("hud_no_key");
    }

    void OnStateChanged(GameManager.GameState state) { }
}
