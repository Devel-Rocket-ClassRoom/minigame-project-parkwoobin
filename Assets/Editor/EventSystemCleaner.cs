using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 모든 씬에서 EventSystem을 SingletonEventSystem으로 교체하는 에디터 툴.
/// Tools > Fix EventSystems In All Scenes
/// </summary>
public static class EventSystemCleaner
{
    [MenuItem("Tools/Fix EventSystems In All Scenes")]
    static void FixAll()
    {
        string currentScene = EditorSceneManager.GetActiveScene().path;
        int fixed_count = 0;

        foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            bool dirty = false;

            foreach (var es in Object.FindObjectsByType<EventSystem>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var go = es.gameObject;

                // SingletonEventSystem이 없으면 추가
                if (go.GetComponent<SingletonEventSystem>() == null)
                {
                    // InputModule 보존
                    var inputModule = go.GetComponent<BaseInputModule>();

                    // 기존 EventSystem 제거 후 SingletonEventSystem 추가
                    Object.DestroyImmediate(es);
                    go.AddComponent<SingletonEventSystem>();

                    Debug.Log($"[EventSystemCleaner] {scene.name} → {go.name} 교체 완료");
                    dirty = true;
                    fixed_count++;
                }
                else if (go.GetComponent<EventSystem>() != null &&
                         go.GetComponent<EventSystem>().GetType() == typeof(EventSystem))
                {
                    // SingletonEventSystem은 있지만 기본 EventSystem도 남아있는 경우
                    Object.DestroyImmediate(go.GetComponent<EventSystem>());
                    Debug.Log($"[EventSystemCleaner] {scene.name} → {go.name} 중복 EventSystem 제거");
                    dirty = true;
                    fixed_count++;
                }
            }

            if (dirty)
                EditorSceneManager.SaveScene(scene);
        }

        if (currentScene != "")
            EditorSceneManager.OpenScene(currentScene, OpenSceneMode.Single);

        EditorUtility.DisplayDialog("완료",
            $"{fixed_count}개 씬에서 EventSystem 정리 완료.", "확인");
    }
}
