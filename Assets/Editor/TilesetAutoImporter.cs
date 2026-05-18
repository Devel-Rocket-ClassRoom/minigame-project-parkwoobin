// TilesetAutoImporter.cs
// Tools > 골목고양이 > 타일셋 자동 임포트 실행
// 1. PNG를 Multiple Sprite로 슬라이스 (불투명 배경 대응)
// 2. 각 스프라이트로 Tile 에셋 생성

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilesetAutoImporter : EditorWindow
{
    const string SOURCE_FOLDER = "Assets/Imported/TileMap";
    const string TILE_FOLDER   = "Assets/Imported/TileMap/Tiles";

    // ── Inspector 설정 ────────────────────────────────────────────────────────
    int   ppu            = 32;   // Pixels Per Unit (Unity 단위 변환)
    int   tileW          = 32;   // 타일 1칸 가로 픽셀
    int   tileH          = 32;   // 타일 1칸 세로 픽셀
    int   offsetX        = 0;    // 이미지 좌측 여백 (px)
    int   offsetY        = 0;    // 이미지 상단 여백 (px)
    int   paddingX       = 0;    // 타일 사이 가로 간격 (px)
    int   paddingY       = 0;    // 타일 사이 세로 간격 (px)
    int   minGap         = 2;
    float colorTolerance = 0.08f;

    enum SliceMode { AutoDetect, UniformGrid }
    SliceMode sliceMode  = SliceMode.UniformGrid;
    bool  snapToGrid     = true;
    bool  showAdvanced   = false;

    // ── GUI ───────────────────────────────────────────────────────────────────

    [MenuItem("Tools/골목고양이/타일셋 자동 임포트")]
    static void Run() => GetWindow<TilesetAutoImporter>("타일셋 임포터").Show();

    void OnGUI()
    {
        GUILayout.Label("타일셋 자동 임포터", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // ── 기본 설정 ──────────────────────────────────────────────────────
        ppu = EditorGUILayout.IntField("Pixels Per Unit", ppu);

        EditorGUILayout.Space();
        GUILayout.Label("── 타일 크기 (px) ──", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("가로 (W)", GUILayout.Width(80));
        tileW = EditorGUILayout.IntField(tileW, GUILayout.Width(60));
        GUILayout.Space(20);
        GUILayout.Label("세로 (H)", GUILayout.Width(80));
        tileH = EditorGUILayout.IntField(tileH, GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        if (tileW != tileH)
            EditorGUILayout.HelpBox(
                $"비정사각형 타일: {tileW}×{tileH}px\n" +
                $"→ Unity 크기: {tileW / (float)ppu:0.##} × {tileH / (float)ppu:0.##} units",
                MessageType.Info);

        // ── 고급 설정 (여백/패딩) ──────────────────────────────────────────
        EditorGUILayout.Space();
        showAdvanced = EditorGUILayout.Foldout(showAdvanced, "고급: 여백·패딩");
        if (showAdvanced)
        {
            EditorGUILayout.HelpBox(
                "이미지 가장자리 여백(Offset)과 타일 사이 간격(Padding)을 지정합니다.\n" +
                "대부분의 경우 0으로 두면 됩니다.",
                MessageType.None);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("이미지 Offset X", GUILayout.Width(120));
            offsetX = EditorGUILayout.IntField(offsetX, GUILayout.Width(50));
            GUILayout.Space(10);
            GUILayout.Label("Y", GUILayout.Width(20));
            offsetY = EditorGUILayout.IntField(offsetY, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("타일 Padding X", GUILayout.Width(120));
            paddingX = EditorGUILayout.IntField(paddingX, GUILayout.Width(50));
            GUILayout.Space(10);
            GUILayout.Label("Y", GUILayout.Width(20));
            paddingY = EditorGUILayout.IntField(paddingY, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();
        }

        // ── 슬라이스 모드 ──────────────────────────────────────────────────
        EditorGUILayout.Space();
        GUILayout.Label("── 슬라이스 모드 ──", EditorStyles.boldLabel);
        sliceMode = (SliceMode)EditorGUILayout.EnumPopup("슬라이스 방식", sliceMode);

        if (sliceMode == SliceMode.UniformGrid)
        {
            EditorGUILayout.HelpBox(
                "이미지를 지정한 타일 크기로 균등하게 나눕니다.\n" +
                "패딩이 있어도 틈 없이 꽉 채워집니다.",
                MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "배경색을 감지해 내용이 있는 영역만 슬라이스합니다.\n" +
                "불규칙한 크기의 스프라이트에 적합합니다.",
                MessageType.Info);
            minGap         = EditorGUILayout.IntField("최소 경계 간격(px)", minGap);
            colorTolerance = EditorGUILayout.Slider("배경색 허용 오차", colorTolerance, 0f, 0.3f);
            snapToGrid     = EditorGUILayout.Toggle("그리드에 스냅", snapToGrid);
            if (snapToGrid)
                EditorGUILayout.HelpBox(
                    $"감지된 rect를 {tileW}×{tileH}px 단위로 반올림 정렬합니다.",
                    MessageType.None);
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("전체 임포트 실행", GUILayout.Height(40)))
            ImportAll();
    }

    // ── 메인 임포트 ───────────────────────────────────────────────────────────

    void ImportAll()
    {
        // 입력값 검증
        tileW = Mathf.Max(1, tileW);
        tileH = Mathf.Max(1, tileH);
        ppu   = Mathf.Max(1, ppu);

        EnsureFolder(TILE_FOLDER);

        string[] pngs = { "1.png", "2.png", "3.png", "4.png", "5.png" };
        int total = 0;

        for (int i = 0; i < pngs.Length; i++)
        {
            string assetPath = $"{SOURCE_FOLDER}/{pngs[i]}";
            if (!File.Exists(ToAbsolute(assetPath))) continue;

            EditorUtility.DisplayProgressBar("타일셋 임포트",
                $"{pngs[i]} 처리 중...", (float)i / pngs.Length);

            string baseName = Path.GetFileNameWithoutExtension(pngs[i]);

            if (sliceMode == SliceMode.UniformGrid)
            {
                ConfigureImporter(assetPath, ppu, readable: false);
                var rects = BuildUniformGrid(assetPath, tileW, tileH, offsetX, offsetY, paddingX, paddingY);
                Debug.Log($"[TilesetAutoImporter] {pngs[i]}: 균등 그리드 {tileW}×{tileH}px → {rects.Count}칸");
                ApplySpriteRects(assetPath, baseName, rects, ppu);
            }
            else
            {
                ConfigureImporter(assetPath, ppu, readable: true);
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (tex == null) { Debug.LogError($"텍스처 로드 실패: {assetPath}"); continue; }

                Color bg  = SampleBackgroundColor(tex);
                var rects = DetectSpriteRects(tex, bg, colorTolerance, minGap);
                Debug.Log($"[TilesetAutoImporter] {pngs[i]}: 배경색={bg}, {rects.Count}개 감지");

                if (snapToGrid)
                {
                    rects = SnapRectsToGrid(rects, tileW, tileH, tex.height);
                    Debug.Log($"[TilesetAutoImporter] {pngs[i]}: 그리드 스냅({tileW}×{tileH}) 후 {rects.Count}개");
                }

                ApplySpriteRects(assetPath, baseName, rects, ppu);
            }

            total += CreateTiles(assetPath, baseName);
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("완료",
            $"총 {total}개 타일 생성!\n위치: {TILE_FOLDER}/", "확인");
        Debug.Log($"[TilesetAutoImporter] 완료: 타일 {total}개");
    }

    // ── 균등 그리드 슬라이스 ─────────────────────────────────────────────────
    // 이미지를 tileW×tileH 크기로 균등하게 나눔.
    // offsetX/Y = 이미지 가장자리 여백, paddingX/Y = 타일 사이 간격.
    //
    // 예) 타일 32×16, 이미지 256×128, 패딩 없음
    //     → 8열 × 8행 = 64개 rect
    //
    // 예) 타일 32×32, 이미지 288×288, offset=4, padding=2
    //     → 유효 영역에서 tileW+paddingX 간격으로 순회

    static List<Rect> BuildUniformGrid(string assetPath,
        int tileW, int tileH,
        int offsetX = 0, int offsetY = 0,
        int paddingX = 0, int paddingY = 0)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        int imgW, imgH;
        imp.GetSourceTextureWidthAndHeight(out imgW, out imgH);

        int stepX = tileW + paddingX;
        int stepY = tileH + paddingY;

        var rects = new List<Rect>();

        for (int py = offsetY; py + tileH <= imgH; py += stepY)
        {
            // Unity Rect Y축: 이미지 위쪽이 높은 y → 뒤집기
            int unityY = imgH - py - tileH;

            for (int px = offsetX; px + tileW <= imgW; px += stepX)
            {
                rects.Add(new Rect(px, unityY, tileW, tileH));
            }
        }

        return rects;
    }

    // ── 그리드 스냅 (AutoDetect 보정용) ──────────────────────────────────────
    // 감지된 rect를 tileW×tileH 단위로 반올림.
    // 예) 32×16 타일셋에서 rect(1, 17, 30, 15) → (0, 16, 32, 16)

    static List<Rect> SnapRectsToGrid(List<Rect> rects, int tileW, int tileH, int texH)
    {
        var snapped = new List<Rect>(rects.Count);
        foreach (var r in rects)
        {
            int sx = Mathf.RoundToInt(r.x     / tileW) * tileW;
            int sy = Mathf.RoundToInt(r.y     / tileH) * tileH;
            int sw = Mathf.Max(tileW, Mathf.RoundToInt(r.width  / tileW) * tileW);
            int sh = Mathf.Max(tileH, Mathf.RoundToInt(r.height / tileH) * tileH);

            sy = Mathf.Clamp(sy, 0, texH - sh);
            snapped.Add(new Rect(sx, sy, sw, sh));
        }
        return snapped;
    }

    // ── 배경색 자동 감지 ──────────────────────────────────────────────────────

    static Color SampleBackgroundColor(Texture2D tex)
    {
        int w = tex.width, h = tex.height;
        int margin = Mathf.Min(8, w / 10, h / 10);

        var samples = new List<Color>();
        int[] xs = { 0, margin, w - 1, w - 1 - margin };
        int[] ys = { 0, margin, h - 1, h - 1 - margin };

        foreach (int x in xs)
            foreach (int y in ys)
                samples.Add(tex.GetPixel(x, y));

        samples.Sort((a, b) => a.grayscale.CompareTo(b.grayscale));
        return samples[samples.Count / 2];
    }

    // ── 스프라이트 렉트 감지 (AutoDetect) ────────────────────────────────────

    static List<Rect> DetectSpriteRects(Texture2D tex, Color bg,
                                         float tolerance, int minGap)
    {
        int w = tex.width, h = tex.height;
        Color[] pixels = tex.GetPixels();

        bool[] rowHas = new bool[h];
        bool[] colHas = new bool[w];

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (!IsBackground(pixels[y * w + x], bg, tolerance))
                { rowHas[y] = true; break; }

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                if (!IsBackground(pixels[y * w + x], bg, tolerance))
                { colHas[x] = true; break; }

        var rowBands = FindBands(rowHas, h, minGap);
        var rects    = new List<Rect>();

        foreach (var (ry_start, ry_end) in rowBands)
        {
            bool[] colInBand = new bool[w];
            for (int x = 0; x < w; x++)
                for (int y = ry_start; y <= ry_end; y++)
                    if (!IsBackground(pixels[y * w + x], bg, tolerance))
                    { colInBand[x] = true; break; }

            var colBands = FindBands(colInBand, w, minGap);

            foreach (var (cx_start, cx_end) in colBands)
            {
                int sprW   = cx_end  - cx_start + 1;
                int sprH   = ry_end  - ry_start + 1;
                int unityY = h - ry_end - 1;
                rects.Add(new Rect(cx_start, unityY, sprW, sprH));
            }
        }

        return rects;
    }

    static bool IsBackground(Color pixel, Color bg, float tolerance)
    {
        if (pixel.a < 0.01f) return true;
        float dr = pixel.r - bg.r;
        float dg = pixel.g - bg.g;
        float db = pixel.b - bg.b;
        return (dr * dr + dg * dg + db * db) < tolerance * tolerance * 3f;
    }

    static List<(int, int)> FindBands(bool[] hasContent, int size, int minGap)
    {
        var bands    = new List<(int, int)>();
        int start    = -1;
        int gapCount = 0;

        for (int i = 0; i < size; i++)
        {
            if (hasContent[i])
            {
                if (start < 0) start = i;
                gapCount = 0;
            }
            else
            {
                if (start >= 0)
                {
                    gapCount++;
                    if (gapCount > minGap)
                    {
                        bands.Add((start, i - gapCount));
                        start    = -1;
                        gapCount = 0;
                    }
                }
            }
        }
        if (start >= 0)
            bands.Add((start, size - 1));

        return bands;
    }

    // ── TextureImporter 설정 ──────────────────────────────────────────────────

    static void ConfigureImporter(string assetPath, int ppu, bool readable)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        if (imp == null) return;

        imp.textureType         = TextureImporterType.Sprite;
        imp.spriteImportMode    = SpriteImportMode.Multiple;
        imp.filterMode          = FilterMode.Point;
        imp.textureCompression  = TextureImporterCompression.Uncompressed;
        imp.spritePixelsPerUnit = ppu;
        imp.isReadable          = readable;
        imp.alphaIsTransparency = true;

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
    }

    static void ApplySpriteRects(string assetPath, string baseName,
                                  List<Rect> rects, int ppu)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        if (imp == null) return;

        var meta = new SpriteMetaData[rects.Count];
        for (int i = 0; i < rects.Count; i++)
        {
            meta[i] = new SpriteMetaData
            {
                name      = $"{baseName}_{i:000}",
                rect      = rects[i],
                alignment = (int)SpriteAlignment.Center,
                pivot     = new Vector2(0.5f, 0.5f)
            };
        }

        imp.spritesheet = meta;
        imp.isReadable  = false;
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
    }

    // ── Tile 에셋 생성 ────────────────────────────────────────────────────────

    static int CreateTiles(string assetPath, string subFolder)
    {
        string dir = $"{TILE_FOLDER}/{subFolder}";
        EnsureFolder(dir);

        var all     = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        int created = 0;

        foreach (var obj in all)
        {
            if (!(obj is Sprite sprite)) continue;

            string tilePath = $"{dir}/{sprite.name}.asset";

            // 기존 에셋 있으면 스프라이트만 교체 (재생성 불필요)
            var existing = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (existing != null)
            {
                existing.sprite = sprite;
                EditorUtility.SetDirty(existing);
                continue;
            }

            var tile    = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.name   = sprite.name;
            AssetDatabase.CreateAsset(tile, tilePath);
            created++;
        }

        AssetDatabase.SaveAssets();
        return created;
    }

    // ── 유틸 ─────────────────────────────────────────────────────────────────

    static void EnsureFolder(string assetPath)
    {
        string abs = ToAbsolute(assetPath);
        if (!Directory.Exists(abs))
        {
            Directory.CreateDirectory(abs);
            AssetDatabase.Refresh();
        }
    }

    static string ToAbsolute(string assetPath) =>
        Path.Combine(Application.dataPath, "..", assetPath);
}
