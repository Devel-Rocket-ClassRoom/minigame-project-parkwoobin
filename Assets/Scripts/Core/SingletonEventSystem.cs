using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// EventSystem을 상속해 OnEnable 시점에 중복을 차단한다.
/// 씬의 EventSystem 컴포넌트를 제거하고 이 컴포넌트로 교체한다.
/// </summary>
public class SingletonEventSystem : EventSystem
{
    static SingletonEventSystem s_instance;

    protected override void OnEnable()
    {
        if (s_instance != null && s_instance != this)
        {
            enabled = false;
            Destroy(gameObject);
            return;
        }

        s_instance = this;
        DontDestroyOnLoad(gameObject);
        base.OnEnable();
    }

    protected override void OnDisable()
    {
        if (s_instance == this) s_instance = null;
        base.OnDisable();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureExists()
    {
        if (FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0)
            return;

        var go = new GameObject("EventSystem");
        go.AddComponent<SingletonEventSystem>();

        var moduleType = System.Type.GetType(
            "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (moduleType != null)
            go.AddComponent(moduleType);
    }
}
