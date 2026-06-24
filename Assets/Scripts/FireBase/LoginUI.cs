using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField]
    private GameObject loginPanel;

    [SerializeField]
    private GameObject loginScreenPanel;

    [SerializeField]
    private GameObject gamePanel;

    [Header("Profile Button")]
    [SerializeField]
    private Button profileButton;

    [Header("Login Form")]
    [SerializeField]
    private TMP_InputField emailInput;

    [SerializeField]
    private TMP_InputField passwordInput;

    [SerializeField]
    private Button loginButton;

    [SerializeField]
    private Button signupButton;

    [SerializeField]
    private Button anonymousButton;

    [SerializeField]
    private TextMeshProUGUI errorText;

    [SerializeField]
    private Button closeButton;

    [Header("References")]
    [SerializeField]
    private ProfileUI profileUI;

    [SerializeField]
    private ProfileEditUI profileEditUI;

    [SerializeField]
    private GameObject signupPanel;

    private void Start()
    {
        profileButton.onClick.AddListener(OnProfileButtonClicked);
        loginButton.onClick.AddListener(() => OnLoginButtonClicked().Forget());
        signupButton.onClick.AddListener(OnSignupButtonClicked);
        anonymousButton.onClick.AddListener(() => OnAnonymousButtonClicked().Forget());
        closeButton?.onClick.AddListener(() => loginPanel.SetActive(false));

        InitializeAsync().Forget();
    }

    private async UniTaskVoid InitializeAsync()
    {
        await UniTask.WaitUntil(() => AuthManager.Instance != null && AuthManager.Instance.IsInitialized);
        AuthManager.Instance.LoginStateChanged += OnLoginStateChanged;
        ApplyLoginState(AuthManager.Instance.IsLogedIn);
    }

    private void OnDestroy()
    {
        if (AuthManager.Instance != null)
            AuthManager.Instance.LoginStateChanged -= OnLoginStateChanged;
    }

    private void OnLoginStateChanged(bool isLoggedIn)
    {
        ApplyLoginState(isLoggedIn);
    }

    private void ApplyLoginState(bool isLoggedIn)
    {
        loginPanel.SetActive(false);
        loginScreenPanel?.SetActive(!isLoggedIn);
        gamePanel?.SetActive(isLoggedIn);
    }

    public async UniTaskVoid OpenLoginPanelAsync()
    {
        await UniTask.WaitUntil(() => AuthManager.Instance != null && AuthManager.Instance.IsInitialized);
        if (!AuthManager.Instance.IsLogedIn)
            loginPanel.SetActive(true);
    }

    public UniTaskVoid UpdateUI()
    {
        if (AuthManager.Instance != null && AuthManager.Instance.IsInitialized)
            ApplyLoginState(AuthManager.Instance.IsLogedIn);
        return default;
    }

    public void AfterLoginFromSignup() => AfterLoginAsync().Forget();

    private async UniTaskVoid AfterLoginAsync()
    {
        loginPanel.SetActive(false);

        if (ProfileManager.Instance != null)
            await UniTask.WaitUntil(() => ProfileManager.Instance.IsInitialized);

        var (profile, _) = await ProfileManager.Instance.LoadProfileAsync();
        bool hasNickname = !string.IsNullOrEmpty(profile?.nickname);

        if (!hasNickname && profileEditUI != null)
            profileEditUI.OpenProfileEditPanelAsync().Forget();
        else
            ApplyLoginState(true);
    }

    private async UniTaskVoid OnLoginButtonClicked()
    {
        string email = emailInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowError("FireBase_InPut_IDPW");
            return;
        }

        if (AuthManager.Instance == null)
        {
            ShowError("FireBase_AuthServiceError");
            return;
        }

        SetButtonsInteractable(false);
        try
        {
            var (success, error) = await AuthManager.Instance.SignInWithEmailAsync(email, password);
            if (success)
                AfterLoginAsync().Forget();
            else
                ShowError(error);
        }
        finally
        {
            SetButtonsInteractable(true);
        }
    }

    private void OnSignupButtonClicked()
    {
        loginPanel.SetActive(false);
        signupPanel?.SetActive(true);
    }

    private async UniTaskVoid OnAnonymousButtonClicked()
    {
        if (AuthManager.Instance == null)
        {
            ShowError("FireBase_AuthServiceError");
            return;
        }

        SetButtonsInteractable(false);
        try
        {
            var (success, error) = await AuthManager.Instance.SignInAnonymouslyAsync();
            if (success)
                AfterLoginAsync().Forget();
            else
                ShowError(error);
        }
        finally
        {
            SetButtonsInteractable(true);
        }
    }

    private void OnProfileButtonClicked()
    {
        if (AuthManager.Instance.IsLogedIn)
            profileUI.OpenProfilePanel().Forget();
        else
            loginPanel.SetActive(true);
    }

    private void ShowError(string message)
    {
        errorText.text = LocalizationManager.Get(message);
        errorText.color = Color.red;
    }

    private void SetButtonsInteractable(bool interactable)
    {
        loginButton.interactable = interactable;
        signupButton.interactable = interactable;
        anonymousButton.interactable = interactable;
    }
}
