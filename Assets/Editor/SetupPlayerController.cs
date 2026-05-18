using UnityEngine;
using UnityEditor;

public static class SetupPlayerController
{
    [MenuItem("Tools/Cat Game/Setup Player Controller")]
    public static void Run()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null) { Debug.LogError("[Setup] Player GameObject not found"); return; }

        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc == null) { Debug.LogError("[Setup] PlayerController not found on Player"); return; }

        Transform groundCheck = player.transform.Find("GroundCheck");
        if (groundCheck == null) { Debug.LogError("[Setup] GroundCheck child not found"); return; }

        SerializedObject so = new SerializedObject(pc);
        so.FindProperty("groundCheck").objectReferenceValue = groundCheck;
        so.FindProperty("groundLayer").intValue = 1 << 7;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(pc);

        Debug.Log("[Setup] PlayerController 설정 완료 — groundCheck: GroundCheck, groundLayer: Ground(128)");
    }

    [MenuItem("Tools/Cat Game/Setup Camera Follow")]
    public static void SetupCameraFollow()
    {
        GameObject cam = GameObject.Find("Main Camera");
        if (cam == null) { Debug.LogError("[Setup] Main Camera not found"); return; }

        GameObject player = GameObject.Find("Player");
        if (player == null) { Debug.LogError("[Setup] Player not found"); return; }

        CameraFollow cf = cam.GetComponent<CameraFollow>();
        if (cf == null) cf = cam.AddComponent<CameraFollow>();

        SerializedObject so = new SerializedObject(cf);
        so.FindProperty("target").objectReferenceValue = player.transform;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(cam);

        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Setup] CameraFollow → Main Camera 연결 완료 (target: Player)");
    }

    [MenuItem("Tools/Cat Game/Assign Idle Sprite")]
    public static void AssignIdleSprite()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null) { Debug.LogError("[Setup] Player GameObject not found"); return; }

        SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
        if (sr == null) { Debug.LogError("[Setup] SpriteRenderer not found on Player"); return; }

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(
            "Assets/Imported/Cat_player/Cat_player/Cat_sheets/Cat_idle_1.png");

        Sprite idleSprite = null;
        foreach (Object a in assets)
        {
            if (a is Sprite s && s.name == "Cat_idle_1_0")
            {
                idleSprite = s;
                break;
            }
        }

        if (idleSprite == null) { Debug.LogError("[Setup] Cat_idle_1_0 스프라이트를 찾지 못했습니다"); return; }

        SerializedObject so = new SerializedObject(sr);
        so.FindProperty("m_Sprite").objectReferenceValue = idleSprite;
        so.FindProperty("m_Color").colorValue = Color.white;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(sr);

        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Setup] Idle 스프라이트 할당 완료: Cat_idle_1_0 → Player SpriteRenderer");
    }
}
