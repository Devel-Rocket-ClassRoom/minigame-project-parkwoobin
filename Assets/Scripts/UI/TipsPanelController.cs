using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 도움말(Tips) 패널.
/// Inspector의 tipKeys 배열에 strings.json 키를 입력하면
/// 현재 언어로 팁 목록을 스크롤 뷰에 표시한다.
/// </summary>
public class TipsPanelController : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] GameObject panelRoot;
    [SerializeField] GameObject dimOverlay;
    [SerializeField] Button closeButton;
    [SerializeField] Transform contentParent;   // ScrollView > Viewport > Content

    [Header("팁 아이템 프리팹")]
    [Tooltip("TMP_Text 컴포넌트를 가진 프리팹. 없으면 코드로 자동 생성.")]
    [SerializeField] GameObject tipItemPrefab;

    [Header("팁 키 목록 (strings.json 키)")]
    [Tooltip("비워두면 tip_0, tip_1, ... 순서로 strings.json에 존재하는 키를 자동으로 읽음")]
    [SerializeField] string[] tipKeys;

    readonly List<GameObject> _items = new();
    public event Action OnShown;
    public event Action OnHidden;
    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

    void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        closeButton?.onClick.AddListener(Hide);
    }

    void OnEnable() => LanguageManager.OnLanguageChanged += OnLanguageChanged;
    void OnDisable() => LanguageManager.OnLanguageChanged -= OnLanguageChanged;

    void OnLanguageChanged(LanguageManager.Language _) => RefreshItems();

    public void Show()
    {
        if (IsBlockedByHudPanel()) return;

        if (panelRoot != null) panelRoot.SetActive(true);
        if (dimOverlay != null) dimOverlay.SetActive(true);
        RefreshItems();
        NotifyHudControllers(true);
        OnShown?.Invoke();
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (dimOverlay != null) dimOverlay.SetActive(false);
        NotifyHudControllers(false);
        OnHidden?.Invoke();
    }

    void RefreshItems()
    {
        // 기존 아이템 제거
        foreach (var item in _items)
            if (item != null) Destroy(item);
        _items.Clear();

        if (contentParent == null) return;

        // tipKeys가 비어있으면 tip_0, tip_1, ... 자동 탐색
        IEnumerable<string> keys = (tipKeys != null && tipKeys.Length > 0)
            ? (IEnumerable<string>)tipKeys
            : AutoTipKeys();

        foreach (string key in keys)
        {
            string text = LocalizationManager.Get(key);
            if (string.IsNullOrWhiteSpace(text)) continue;

            var item = CreateItem($"• {text}");
            _items.Add(item);
        }
    }

    // tip_0, tip_1, ... 키를 strings.json에 존재하는 만큼 반환
    static IEnumerable<string> AutoTipKeys()
    {
        for (int i = 0; ; i++)
        {
            string key = $"tip_{i}";
            string text = LocalizationManager.Get(key);
            if (string.IsNullOrWhiteSpace(text)) yield break;
            yield return key;
        }
    }

    GameObject CreateItem(string text)
    {
        GameObject go;

        if (tipItemPrefab != null)
        {
            go = Instantiate(tipItemPrefab, contentParent);
        }
        else
        {
            // 프리팹 없으면 TMP 텍스트 오브젝트 자동 생성
            go = new GameObject("TipItem", typeof(RectTransform));
            go.transform.SetParent(contentParent, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 30;
            tmp.color = Color.white;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.margin = new Vector4(20, 8, 20, 8);

            // ContentSizeFitter: 텍스트 길이에 맞게 높이 자동 조절
            var csf = go.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        var label = go.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null) label.text = text;

        return go;
    }

    bool IsBlockedByHudPanel()
    {
        foreach (var shop in FindObjectsByType<ShopPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (shop != null && shop.IsVisible) return true;

        foreach (var hud in FindObjectsByType<HUDController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (hud != null && hud.IsAnyPanelOpen()) return true;

        return false;
    }

    void NotifyHudControllers(bool visible) { /* 도움말은 HUDController를 거치지 않음 */ }
}
