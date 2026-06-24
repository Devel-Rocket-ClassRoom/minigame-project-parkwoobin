using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class FireBaseManager : MonoBehaviour
{
    public static FireBaseManager Instance { get; private set; }

    [Header("Login Screen Buttons")]
    [SerializeField] private Button loginButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button ProfileButton;

    [Header("References")]
    [SerializeField] private LoginUI loginUI;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        loginButton?.onClick.AddListener(() => loginUI?.OpenLoginPanelAsync().Forget());
        exitButton?.onClick.AddListener(OnExitButtonClicked);

        ProfileButton?.gameObject.SetActive(false);
        InitializeAsync().Forget();
    }

    private async UniTaskVoid InitializeAsync()
    {
        await UniTask.WaitUntil(() => AuthManager.Instance != null && AuthManager.Instance.IsInitialized);
        AuthManager.Instance.LoginStateChanged += OnLoginStateChanged;
        OnLoginStateChanged(AuthManager.Instance.IsLogedIn);
    }

    private void OnLoginStateChanged(bool isLoggedIn)
    {
        ProfileButton?.gameObject.SetActive(isLoggedIn);
    }

    private void OnDestroy()
    {
        if (AuthManager.Instance != null)
            AuthManager.Instance.LoginStateChanged -= OnLoginStateChanged;
    }

    private void OnExitButtonClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
