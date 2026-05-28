using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// EventSystem 오브젝트에 붙인다.
/// 첫 번째 EventSystem을 DontDestroyOnLoad로 유지하고,
/// 씬 로드 시 새로 생긴 중복 EventSystem을 즉시 제거한다.
///
/// Main 씬처럼 EventSystem이 없는 씬을 직접 실행할 때는
/// EnsureExists()가 자동으로 EventSystem을 생성한다.
/// </summary>
[DefaultExecutionOrder(-100)]
public class SingletonEventSystem : MonoBehaviour
{
    void Awake()
    {
        var all = FindObjectsByType<EventSystem>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (all.Length > 1)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var all = FindObjectsByType<EventSystem>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var es in all)
        {
            if (es.gameObject == this.gameObject) continue;
            es.gameObject.SetActive(false);
            Destroy(es.gameObject);
        }
    }

    /// <summary>
    /// 씬에 EventSystem이 없을 때 자동 생성.
    /// Main 씬처럼 EventSystem을 제거한 씬을 직접 실행할 때 동작한다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureExists()
    {
        if (FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0)
            return;

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();

        var moduleType = System.Type.GetType(
            "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (moduleType != null)
            go.AddComponent(moduleType);

        go.AddComponent<SingletonEventSystem>();
    }
}
