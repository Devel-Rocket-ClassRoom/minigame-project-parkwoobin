using Cysharp.Threading.Tasks;
using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

public class ProfileEditUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField]
    private GameObject profileEditPanel;

    [SerializeField]
    private GameObject createProfilePanel;

    [SerializeField]
    private GameObject editProfilePanel;

    [Header("Create Profile")]
    [SerializeField]
    private TMP_InputField createNicknameInput;

    [SerializeField]
    private Button createButton;

    [SerializeField]
    private TextMeshProUGUI createErrorText;

    [Header("Edit Profile")]
    [SerializeField]
    private TextMeshProUGUI currentNicknameText;

    [SerializeField]
    private TMP_InputField editNicknameInput;

    [SerializeField]
    private Button updateButton;

    [SerializeField]
    private Button closeEditButton;

    [SerializeField]
    private TextMeshProUGUI editErrorText;

    [Header("References")]
    [SerializeField]
    private ProfileUI profileUI;

    [SerializeField]
    private LoginUI loginUI;

    private string signupPendingEmail;
    private string signupPendingPassword;
    private bool isSignupMode;

    private void Start()
    {
        createButton.onClick.AddListener(() => OnCreateButtonClicked().Forget());
        updateButton.onClick.AddListener(() => OnUpdateButtonClicked().Forget());
        closeEditButton.onClick.AddListener(OnCloseEditButtonClicked);

        profileEditPanel.SetActive(false);
    }

    public void OpenForSignup(string email, string password, LoginUI loginUIRef)
    {
        signupPendingEmail = email;
        signupPendingPassword = password;
        loginUI = loginUIRef;
        isSignupMode = true;

        createNicknameInput.text = "";
        createErrorText.text = "";
        createProfilePanel.SetActive(true);
        editProfilePanel.SetActive(false);
        profileEditPanel.SetActive(true);
    }

    public async UniTaskVoid OpenProfileEditPanelAsync()
    {
        profileEditPanel.SetActive(true);

        if (ProfileManager.Instance == null)
        {
            ShowCreateProfile();
            return;
        }

        var (profile, _) = await ProfileManager.Instance.LoadProfileAsync();

        if (profile != null)
            ShowEditProfile(profile);
        else
            ShowCreateProfile();
    }

    private void ShowCreateProfile()
    {
        createProfilePanel.SetActive(true);
        editProfilePanel.SetActive(false);
        createNicknameInput.text = "";
        createErrorText.text = "";
    }

    private void ShowEditProfile(UserProfile profile)
    {
        createProfilePanel.SetActive(false);
        editProfilePanel.SetActive(true);

        currentNicknameText.text = LocalizationManager.Get("FireBase_CurrentNickname") + profile.nickname;
        editNicknameInput.text = profile.nickname;
        editErrorText.text = "";
    }

    private async UniTaskVoid OnCreateButtonClicked()
    {
        string nickname = createNicknameInput.text.Trim();

        if (string.IsNullOrEmpty(nickname))
        {
            createErrorText.text = LocalizationManager.Get("FireBase_EnterNickname");
            createErrorText.color = Color.red;
            return;
        }

        if (ProfileManager.Instance == null)
        {
            createErrorText.text = LocalizationManager.Get("FireBase_ProfileServiceError");
            createErrorText.color = Color.red;
            return;
        }

        createButton.interactable = false;
        try
        {
            if (isSignupMode)
            {
                if (AuthManager.Instance == null)
                {
                    createErrorText.text = LocalizationManager.Get("FireBase_AuthServiceError");
                    createErrorText.color = Color.red;
                    return;
                }
                var (authSuccess, authError) = await AuthManager.Instance.CreateUserWithEmailAsync(signupPendingEmail, signupPendingPassword);
                if (!authSuccess)
                {
                    createErrorText.text = authError;
                    createErrorText.color = Color.red;
                    return;
                }
                if (ProfileManager.Instance != null)
                    await UniTask.WaitUntil(() => ProfileManager.Instance.IsInitialized);
            }

            var (success, error) = await ProfileManager.Instance.SaveProfileAsync(nickname);

            if (success)
            {
                createErrorText.text = LocalizationManager.Get("FireBase_ProfileCreated");
                createErrorText.color = Color.green;

                profileEditPanel.SetActive(false);
                isSignupMode = false;

                await UniTask.Delay(TimeSpan.FromSeconds(1));
                profileUI?.UpdateProfileUIAsync().Forget();
                loginUI?.UpdateUI().Forget();
            }
            else
            {
                createErrorText.text = error;
                createErrorText.color = Color.red;
            }
        }
        finally
        {
            createButton.interactable = true;
        }
    }

    private async UniTaskVoid OnUpdateButtonClicked()
    {
        string nickname = editNicknameInput.text.Trim();

        if (string.IsNullOrEmpty(nickname))
        {
            editErrorText.text = LocalizationManager.Get("FireBase_EnterNickname");
            editErrorText.color = Color.red;

            return;
        }

        updateButton.interactable = false;
        var (success, error) = await ProfileManager.Instance.UpdateNicknameAsync(nickname);

        if (success)
        {
            editErrorText.text = LocalizationManager.Get("FireBase_EditComplete");
            editErrorText.color = Color.green;
            currentNicknameText.text = LocalizationManager.Get("FireBase_CurrentNickname") + nickname;

            await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: this.GetCancellationTokenOnDestroy());
            profileEditPanel.SetActive(false);

            profileUI?.OpenProfilePanel().Forget();
        }
        else
        {
            editErrorText.text = error;
            editErrorText.color = Color.red;
        }
        updateButton.interactable = true;
    }

    private void OnCloseEditButtonClicked()
    {
        profileEditPanel.SetActive(false);
    }
}
