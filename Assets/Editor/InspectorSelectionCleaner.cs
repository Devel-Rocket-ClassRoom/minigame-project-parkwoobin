using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 씬 전환/리로드 시 Inspector 선택을 초기화해
/// "destroyed object still accessed" 에디터 오류를 방지한다.
/// </summary>
[InitializeOnLoad]
static class InspectorSelectionCleaner
{
    static InspectorSelectionCleaner()
    {
        EditorSceneManager.sceneClosing += (_, _) => Selection.activeObject = null;
    }
}
