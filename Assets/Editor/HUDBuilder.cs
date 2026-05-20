using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public static class HUDBuilder
{
    const string FILL_PATH  = "Assets/Imported/Sprites/UI/HealthBar_Fill_v2.png";
    const string FRAME_PATH = "Assets/Imported/Sprites/UI/HealthBar_Frame_v2.png";

    [MenuItem("Tools/Build HUD Canvas")]
    public static void BuildHUD()
    {
        var existing = GameObject.Find("HUD_Canvas");
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);

        // 스프라이트 로드
        var spriteFill  = AssetDatabase.LoadAssetAtPath<Sprite>(FILL_PATH);
        var spriteFrame = AssetDatabase.LoadAssetAtPath<Sprite>(FRAME_PATH);

        // ── Canvas ──────────────────────────────────────────────────────────
        var canvasGO = new GameObject("HUD_Canvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Build HUD");

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0f;

        canvasGO.AddComponent<GraphicRaycaster>();

        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(esGO, "Build HUD");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // ── 좌측 상단 패널 (HP + 배고픔) ────────────────────────────────────
        var topLeft = CreatePanel("TopLeft_Panel", canvasGO.transform,
            anchorMin:     new Vector2(0f, 1f),
            anchorMax:     new Vector2(0f, 1f),
            pivot:         new Vector2(0f, 1f),
            anchoredPos:   new Vector2(20f, -20f),
            size:          new Vector2(280f, 110f));

        // HP 슬라이더 행
        var hpRow = CreateRow("HP_Row", topLeft.transform,
            yOffset: 0f, rowHeight: 48f);
        CreateRowLabel("HP_Label",      hpRow.transform, "HP",   22);
        var hpSlider = CreateBarSlider("HP_Slider", hpRow.transform,
            spriteFill, spriteFrame, new Color(0.95f, 0.15f, 0.15f));

        // 배고픔 슬라이더 행
        var hungerRow = CreateRow("Hunger_Row", topLeft.transform,
            yOffset: -56f, rowHeight: 48f);
        CreateRowLabel("Hunger_Label",  hungerRow.transform, "Food", 22);
        var hungerSlider = CreateBarSlider("Hunger_Slider", hungerRow.transform,
            spriteFill, spriteFrame, new Color(0.95f, 0.60f, 0.10f));

        // ── 우측 상단 패널 (스테이지 + 미니맵) ──────────────────────────────
        var topRight = CreatePanel("TopRight_Panel", canvasGO.transform,
            anchorMin:     new Vector2(1f, 1f),
            anchorMax:     new Vector2(1f, 1f),
            pivot:         new Vector2(1f, 1f),
            anchoredPos:   new Vector2(-20f, -20f),
            size:          new Vector2(200f, 260f));

        // 스테이지 텍스트
        var stageGO = CreateTMPText("Stage_Text", topRight.transform, "Stage 1", 28);
        var stageRect = stageGO.GetComponent<RectTransform>();
        stageRect.anchorMin = new Vector2(0f, 1f);
        stageRect.anchorMax = new Vector2(1f, 1f);
        stageRect.pivot     = new Vector2(0.5f, 1f);
        stageRect.anchoredPosition = new Vector2(0f, -4f);
        stageRect.sizeDelta = new Vector2(0f, 36f);

        // 미니맵 프레임
        var minimapFrame = CreatePanel("Minimap_Frame", topRight.transform,
            anchorMin:     new Vector2(0f, 0f),
            anchorMax:     new Vector2(1f, 1f),
            pivot:         new Vector2(0.5f, 0.5f),
            anchoredPos:   new Vector2(0f, -24f),
            size:          new Vector2(0f, -44f));
        minimapFrame.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

        // 미니맵 RawImage
        var minimapGO = new GameObject("Minimap_RawImage");
        Undo.RegisterCreatedObjectUndo(minimapGO, "Build HUD");
        minimapGO.transform.SetParent(minimapFrame.transform, false);
        var rawImg  = minimapGO.AddComponent<RawImage>();
        rawImg.color = Color.white;
        // 1:1 비율 강제 — RT(256×256)를 찌부 없이 표시
        var arf = minimapGO.AddComponent<AspectRatioFitter>();
        arf.aspectMode  = AspectRatioFitter.AspectMode.FitInParent;
        arf.aspectRatio = 1f;
        var rawRect  = minimapGO.GetComponent<RectTransform>();
        rawRect.anchorMin = Vector2.zero;
        rawRect.anchorMax = Vector2.one;
        rawRect.offsetMin = new Vector2(5f, 5f);
        rawRect.offsetMax = new Vector2(-5f, -5f);

        // ── HUDController 연결 ──────────────────────────────────────────────
        var hud = canvasGO.AddComponent<HUDController>();
        var so  = new SerializedObject(hud);
        so.FindProperty("hpSlider").objectReferenceValue     = hpSlider;
        so.FindProperty("hungerSlider").objectReferenceValue = hungerSlider;
        so.FindProperty("stageText").objectReferenceValue    = stageGO.GetComponent<TextMeshProUGUI>();
        so.FindProperty("minimapImage").objectReferenceValue = rawImg;
        so.ApplyModifiedProperties();

        // ── HungerSystem → GameManager에 추가 ───────────────────────────────
        var gmGO = GameObject.Find("GameManager");
        if (gmGO != null && gmGO.GetComponent<HungerSystem>() == null)
            Undo.AddComponent<HungerSystem>(gmGO);

        BuildMinimapCamera(rawImg);

        EditorUtility.SetDirty(canvasGO);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[HUDBuilder] HUD_Canvas 생성 완료!");
        Selection.activeGameObject = canvasGO;
    }

    // ── 미니맵만 단독 수정 (Canvas 건드리지 않음) ────────────────────────────
    [MenuItem("Tools/Fix Minimap Only")]
    public static void FixMinimapOnly()
    {
        var rawImgGO = GameObject.Find("Minimap_RawImage");
        var rawImg   = rawImgGO != null ? rawImgGO.GetComponent<RawImage>() : null;

        // RT를 200×130으로 재생성
        if (AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/MinimapRT.renderTexture") != null)
            AssetDatabase.DeleteAsset("Assets/MinimapRT.renderTexture");

        // 2배 해상도 + Point 필터 → 픽셀아트 선명도 유지
        var rt = new RenderTexture(400, 260, 16, RenderTextureFormat.ARGB32)
        {
            name = "MinimapRT",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        rt.Create();
        AssetDatabase.CreateAsset(rt, "Assets/MinimapRT.renderTexture");
        AssetDatabase.SaveAssets();
        rt = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/MinimapRT.renderTexture");

        // 카메라 & RawImage에 새 RT 할당
        var camGO = GameObject.Find("MinimapCamera");
        if (camGO != null)
        {
            var cam = camGO.GetComponent<Camera>();
            if (cam != null) { cam.targetTexture = rt; EditorUtility.SetDirty(camGO); }
        }
        if (rawImg != null)
        {
            rawImg.texture = rt;
            rawImg.color   = Color.white;
            EditorUtility.SetDirty(rawImg.gameObject);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("[HUDBuilder] 미니맵 RT 200×130 재생성 완료!");
    }

    // ── 미니맵 카메라 ────────────────────────────────────────────────────────
    static void BuildMinimapCamera(RawImage rawImg)
    {
        var existing = GameObject.Find("MinimapCamera");
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);

        // 기존 RT 삭제 후 새로 만들기
        if (AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/MinimapRT.renderTexture") != null)
            AssetDatabase.DeleteAsset("Assets/MinimapRT.renderTexture");

        // RT 생성 → 저장 → 디스크에서 재로드 (직렬화 보장)
        var rt = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32)
        {
            name = "MinimapRT",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        rt.Create();
        AssetDatabase.CreateAsset(rt, "Assets/MinimapRT.renderTexture");
        AssetDatabase.SaveAssets();
        rt = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/MinimapRT.renderTexture");

        var camGO = new GameObject("MinimapCamera");
        Undo.RegisterCreatedObjectUndo(camGO, "Build HUD");
        camGO.transform.position = new Vector3(0f, 0f, -10f);

        var cam = camGO.AddComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = 15f;
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = new Color(0.08f, 0.08f, 0.08f, 1f);
        cam.cullingMask      = -1;   // 모든 레이어 렌더링
        cam.depth            = -2f;
        cam.targetTexture    = rt;

        // URP 카메라 데이터 — Base 타입으로 RT에 독립 렌더
        var urpCamData = camGO.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        urpCamData.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Base;

        var mc = camGO.AddComponent<MinimapCamera>();
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var mso = new SerializedObject(mc);
            mso.FindProperty("target").objectReferenceValue = player.transform;
            mso.ApplyModifiedProperties();
        }

        // RawImage에 RT 할당 + 완전 불투명
        if (rawImg != null)
        {
            rawImg.texture = rt;
            rawImg.color   = Color.white;
            EditorUtility.SetDirty(rawImg);
        }

        EditorUtility.SetDirty(camGO);
    }

    // ── 헬퍼 ──────────────────────────────────────────────────────────────────

    static GameObject CreatePanel(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Build HUD");
        go.transform.SetParent(parent, false);
        var img  = go.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.45f);
        var rect  = go.GetComponent<RectTransform>();
        rect.anchorMin        = anchorMin;
        rect.anchorMax        = anchorMax;
        rect.pivot            = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta        = size;
        return go;
    }

    // 고정 높이 행 (부모 너비 100% 사용)
    static GameObject CreateRow(string name, Transform parent, float yOffset, float rowHeight)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Build HUD");
        go.transform.SetParent(parent, false);
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing              = 8f;
        hlg.padding              = new RectOffset(8, 8, 4, 4);
        hlg.childAlignment       = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth    = false;
        hlg.childControlHeight   = true;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin        = new Vector2(0f, 1f);
        rect.anchorMax        = new Vector2(1f, 1f);
        rect.pivot            = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(0f, yOffset);
        rect.sizeDelta        = new Vector2(0f, rowHeight);
        return go;
    }

    static void CreateRowLabel(string name, Transform parent, string text, int fontSize)
    {
        var go  = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Build HUD");
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth  = 44f;
        le.flexibleWidth   = 0f;
    }

    // 커스텀 스프라이트를 사용하는 슬라이더
    static Slider CreateBarSlider(string name, Transform parent,
        Sprite spriteFill, Sprite spriteFrame, Color fillTint)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Build HUD");
        go.transform.SetParent(parent, false);
        var slider  = go.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value    = 1f;
        slider.interactable = false;
        var le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;

        // Background (프레임 스프라이트)
        var bgGO   = new GameObject("Background");
        Undo.RegisterCreatedObjectUndo(bgGO, "Build HUD");
        bgGO.transform.SetParent(go.transform, false);
        var bgImg  = bgGO.AddComponent<Image>();
        if (spriteFrame != null)
        {
            bgImg.sprite = spriteFrame;
            bgImg.type   = Image.Type.Sliced;
        }
        bgImg.color = Color.white;
        var bgRect  = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        slider.targetGraphic = bgImg;

        // Fill Area — 좌우 패딩만 주고 세로는 중앙 50%
        var faGO   = new GameObject("Fill Area");
        Undo.RegisterCreatedObjectUndo(faGO, "Build HUD");
        faGO.transform.SetParent(go.transform, false);
        var faRect = faGO.AddComponent<RectTransform>();
        faRect.anchorMin        = new Vector2(0f, 0.25f);
        faRect.anchorMax        = new Vector2(1f, 0.75f);
        faRect.offsetMin        = new Vector2(4f, 0f);
        faRect.offsetMax        = new Vector2(-4f, 0f);

        // Fill — pivot (0, 0.5), anchorMax.x = 0 이어야 Slider가 너비를 제어함
        var fillGO  = new GameObject("Fill");
        Undo.RegisterCreatedObjectUndo(fillGO, "Build HUD");
        fillGO.transform.SetParent(faGO.transform, false);
        var fillImg = fillGO.AddComponent<Image>();
        if (spriteFill != null)
        {
            fillImg.sprite = spriteFill;
            fillImg.type   = Image.Type.Sliced;
        }
        fillImg.color = fillTint;
        var fillRect  = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin        = new Vector2(0f, 0f);
        fillRect.anchorMax        = new Vector2(0f, 1f);   // x = 0 필수
        fillRect.pivot            = new Vector2(0f, 0.5f); // 왼쪽 기준으로 확장
        fillRect.sizeDelta        = Vector2.zero;

        slider.fillRect = fillRect;
        return slider;
    }

    static GameObject CreateTMPText(string name, Transform parent, string text, int fontSize)
    {
        var go  = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Build HUD");
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        return go;
    }
}
