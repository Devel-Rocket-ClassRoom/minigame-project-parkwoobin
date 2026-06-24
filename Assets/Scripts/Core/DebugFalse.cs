using UnityEngine;

public class DebugFalse : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        Debug.unityLogger.logEnabled = false;
    }
}
