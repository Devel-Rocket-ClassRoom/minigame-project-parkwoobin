using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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

    [Header("상점")]
    [SerializeField] Button shopButton;
    [SerializeField] ShopPanel shopPanel;

    PlayerController _player;

    int _keys;  // 언어 변경 시 키 텍스트 재구성용
    bool _subscribedToShopPanel;
    bool _hudMenuButtonsResolved;
    readonly List<GameObject> _hudMenuButtonObjects = new();

    void OnEnable()
    {
        HungerSystem.OnHungerChanged += OnHungerChanged;
        GameManager.OnStateChanged += OnStateChanged;
        CoinKeySystem.OnCoinChanged += OnCoinChanged;
        CoinKeySystem.OnKeyChanged += OnKeyChanged;
        LanguageManager.OnLanguageChanged += OnLanguageChanged;
        SubscribeShopPanel();
    }

    void OnDisable()
    {
        HungerSystem.OnHungerChanged -= OnHungerChanged;
        GameManager.OnStateChanged -= OnStateChanged;
        CoinKeySystem.OnCoinChanged -= OnCoinChanged;
        CoinKeySystem.OnKeyChanged -= OnKeyChanged;
        LanguageManager.OnLanguageChanged -= OnLanguageChanged;

        UnsubscribeShopPanel();
    }

    void OnLanguageChanged(LanguageManager.Language _) => RefreshKeyText();

    void Start()
    {
        _player = FindFirstObjectByType<PlayerController>();
        RefreshHP();

        if (settingPanel != null) settingPanel.SetActive(false);
        if (settingButton != null) settingButton.onClick.AddListener(OnSettingClick);
        if (settingCloseButton != null) settingCloseButton.onClick.AddListener(CloseSettingPanel);

        if (shopPanel == null)
            shopPanel = FindFirstObjectByType<ShopPanel>(FindObjectsInactive.Include);

        SubscribeShopPanel();

        shopButton?.onClick.AddListener(OnShopClick);
        ResolveHudMenuButtons();
        SetHudMenuButtonsActive(true);

        // 씬 진입 시 업그레이드 효과 적용
        UpgradeManager.Instance?.ApplyToPlayer();
    }

    public void OnSettingClick()
    {
        if (IsAnyHudPanelOpen()) return;
        if (settingPanel == null) return;

        bool open = !settingPanel.activeSelf;
        settingPanel?.SetActive(open);
        SetPause(open);
        SetHudMenuButtonsActive(!open);
    }

    public void CloseSettingPanel()
    {
        settingPanel?.SetActive(false);
        SetPause(false);
        SetHudMenuButtonsActive(true);
    }

    void OnShopClick()
    {
        if (IsAnyHudPanelOpen()) return;

        if (shopPanel == null) return;
        shopPanel.Show();
    }

    void OnShopShown()
    {
        SetPause(true);
        SetHudMenuButtonsActive(false);
    }

    void OnShopHidden()
    {
        bool settingOpen = settingPanel != null && settingPanel.activeSelf;
        SetPause(settingOpen);
        SetHudMenuButtonsActive(!settingOpen);
    }

    void SubscribeShopPanel()
    {
        if (shopPanel == null || _subscribedToShopPanel) return;
        shopPanel.OnShown += OnShopShown;
        shopPanel.OnHidden += OnShopHidden;
        _subscribedToShopPanel = true;
    }

    void UnsubscribeShopPanel()
    {
        if (shopPanel == null || !_subscribedToShopPanel) return;
        shopPanel.OnShown -= OnShopShown;
        shopPanel.OnHidden -= OnShopHidden;
        _subscribedToShopPanel = false;
    }

    bool IsAnyHudPanelOpen()
    {
        bool settingOpen = settingPanel != null && settingPanel.activeSelf;
        bool shopOpen = shopPanel != null && shopPanel.IsVisible;
        return settingOpen || shopOpen;
    }

    /// <summary>외부(TipsPanel 등)에서 다른 패널이 열려있는지 확인할 때 사용.</summary>
    public bool IsAnyPanelOpen() => IsAnyHudPanelOpen();

    void ResolveHudMenuButtons()
    {
        if (_hudMenuButtonsResolved) return;
        _hudMenuButtonsResolved = true;

        AddHudMenuButton(settingButton);
        AddHudMenuButton(shopButton);
        AddSiblingButtons(settingButton);
        AddSiblingButtons(shopButton);
    }

    void AddSiblingButtons(Button source)
    {
        if (source == null || source.transform.parent == null) return;

        foreach (var button in source.transform.parent.GetComponentsInChildren<Button>(true))
            AddHudMenuButton(button);
    }

    void AddHudMenuButton(Button button)
    {
        if (button == null) return;
        if (settingCloseButton != null && button == settingCloseButton) return;

        var go = button.gameObject;
        if (go == null || _hudMenuButtonObjects.Contains(go)) return;
        _hudMenuButtonObjects.Add(go);
    }

    void SetHudMenuButtonsActive(bool active)
    {
        ResolveHudMenuButtons();

        foreach (var go in _hudMenuButtonObjects)
        {
            if (go != null)
                go.SetActive(active);
        }
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
