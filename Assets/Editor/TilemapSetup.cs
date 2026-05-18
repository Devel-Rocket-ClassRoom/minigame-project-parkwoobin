using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using UnityEditor.SceneManagement;
using System.IO;

public static class TilemapSetup
{
    const string TILE_DIR  = "Assets/Tiles";
    const string TILES_SRC = "Assets/Imported/alley_cat_tilemap_pack/individual_tiles_32px";

    // 사용할 타일 목록: (파일명prefix, 설명)
    static readonly string[] TileNames = new string[]
    {
        "08_village_concrete",
        "09_village_concrete_crack",
        "10_village_brick_wall",
        "11_village_brick_wall_alt",
        "12_village_curb",
        "32_downtown_sidewalk",
        "13_road_asphalt",
    };

    [MenuItem("Tools/Cat Game/Create Ground Tilemap")]
    public static void CreateGroundTilemap()
    {
        // 1. 타일 PNG 임포트 설정 수정 (PPU=32, Point filter)
        FixTileImports();

        // 2. Tile 에셋 생성
        AssetDatabase.Refresh();
        CreateTileAssets();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 3. 씬에 Tilemap 오브젝트 구성 및 타일 페인트
        BuildSceneTilemap();

        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[TilemapSetup] 완료: Ground Tilemap 생성됨");
    }

    // ────────────────────────────────────────────
    // 1. 임포트 설정 수정
    // ────────────────────────────────────────────
    static void FixTileImports()
    {
        foreach (string name in TileNames)
        {
            string path = $"{TILES_SRC}/{name}.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { Debug.LogWarning($"[TilemapSetup] PNG 없음: {path}"); continue; }

            importer.textureType          = TextureImporterType.Sprite;
            importer.spriteImportMode     = SpriteImportMode.Single;
            importer.spritePixelsPerUnit  = 32;
            importer.filterMode           = FilterMode.Point;
            importer.textureCompression   = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled        = false;
            importer.SaveAndReimport();
        }
        Debug.Log("[TilemapSetup] 타일 PNG 임포트 설정 수정 완료 (PPU=32, Point)");
    }

    // ────────────────────────────────────────────
    // 2. Tile 에셋 (.asset) 생성
    // ────────────────────────────────────────────
    static void CreateTileAssets()
    {
        if (!Directory.Exists(TILE_DIR))
            AssetDatabase.CreateFolder("Assets", "Tiles");

        foreach (string name in TileNames)
        {
            string assetPath = $"{TILE_DIR}/{name}.asset";
            if (AssetDatabase.LoadAssetAtPath<Tile>(assetPath) != null) continue;

            string spritePath = $"{TILES_SRC}/{name}.png";
            Sprite spr = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (spr == null) { Debug.LogWarning($"[TilemapSetup] 스프라이트 없음: {spritePath}"); continue; }

            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = spr;
            AssetDatabase.CreateAsset(tile, assetPath);
        }
        Debug.Log("[TilemapSetup] Tile 에셋 생성 완료");
    }

    // ────────────────────────────────────────────
    // 3. 씬 구성
    // ────────────────────────────────────────────
    static void BuildSceneTilemap()
    {
        // 기존 플레이스홀더 Ground 비활성화
        GameObject oldGround = GameObject.Find("Ground");
        if (oldGround != null)
        {
            oldGround.SetActive(false);
            EditorUtility.SetDirty(oldGround);
            Debug.Log("[TilemapSetup] 기존 Ground 오브젝트 비활성화");
        }

        // 이미 Grid가 있으면 제거 후 재생성
        GameObject existingGrid = GameObject.Find("Grid");
        if (existingGrid != null) Object.DestroyImmediate(existingGrid);

        // Grid 오브젝트 (Y=-1 배치 → 타일 최상단이 World Y=0)
        GameObject gridGO = new GameObject("Grid");
        gridGO.transform.position = new Vector3(0f, -1f, 0f);
        var grid = gridGO.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f);

        // Ground 타일맵 (Ground 레이어, Ground 태그)
        GameObject groundGO = new GameObject("Ground_Tilemap");
        groundGO.transform.SetParent(gridGO.transform, false);
        groundGO.layer = 7;       // Ground
        groundGO.tag   = "Ground";

        var tilemap  = groundGO.AddComponent<Tilemap>();
        var renderer = groundGO.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = -1;

        // 물리: Static Rigidbody2D + TilemapCollider2D + CompositeCollider2D
        var rb = groundGO.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        var tc = groundGO.AddComponent<TilemapCollider2D>();
        tc.compositeOperation = Collider2D.CompositeOperation.Merge;

        groundGO.AddComponent<CompositeCollider2D>();

        // 배경 벽 타일맵 (충돌 없음)
        GameObject bgGO = new GameObject("Background_Tilemap");
        bgGO.transform.SetParent(gridGO.transform, false);
        var bgTilemap  = bgGO.AddComponent<Tilemap>();
        var bgRenderer = bgGO.AddComponent<TilemapRenderer>();
        bgRenderer.sortingOrder = -2;

        // 타일 로드
        Tile concrete   = Load("08_village_concrete");
        Tile crack      = Load("09_village_concrete_crack");
        Tile brick      = Load("10_village_brick_wall");
        Tile brickAlt   = Load("11_village_brick_wall_alt");
        Tile curb       = Load("12_village_curb");
        Tile sidewalk   = Load("32_downtown_sidewalk");

        // ── 지면 페인트 ──────────────────────────────────
        // Ground_Tilemap: y=0 표면(보도블록), y=-1~-3 채우기(벽돌)
        // 배경 타일맵: y=1~4 뒷벽(벽돌)
        // 가로 범위: x = -25 ~ 25 (51 타일)

        for (int x = -25; x <= 25; x++)
        {
            // 표면 줄 — curb(가장자리)와 sidewalk(보도블록) 섞기
            Tile surfaceTile = (x == -25 || x == 25) ? curb : sidewalk;
            tilemap.SetTile(new Vector3Int(x, 0, 0), surfaceTile);

            // 채우기 — 구체 / 벽돌 교차
            tilemap.SetTile(new Vector3Int(x, -1, 0), concrete);
            tilemap.SetTile(new Vector3Int(x, -2, 0), (x % 2 == 0) ? brick : brickAlt);
            tilemap.SetTile(new Vector3Int(x, -3, 0), brick);

            // 배경 뒷벽
            for (int y = 1; y <= 5; y++)
                bgTilemap.SetTile(new Vector3Int(x, y, 0), (y % 2 == 0) ? brickAlt : brick);
        }

        // crack 장식 (랜덤 느낌으로 고정 위치에 몇 개)
        if (crack != null)
        {
            int[] crackPositions = { -18, -9, 0, 7, 17 };
            foreach (int cx in crackPositions)
                tilemap.SetTile(new Vector3Int(cx, 0, 0), crack);
        }

        EditorUtility.SetDirty(groundGO);
        EditorUtility.SetDirty(bgGO);
        Debug.Log("[TilemapSetup] 씬 타일맵 페인트 완료 (51 타일 너비 × 4층 깊이 + 배경벽 5층)");
    }

    static Tile Load(string name)
    {
        Tile t = AssetDatabase.LoadAssetAtPath<Tile>($"{TILE_DIR}/{name}.asset");
        if (t == null) Debug.LogWarning($"[TilemapSetup] Tile 에셋 없음: {name}");
        return t;
    }
}
