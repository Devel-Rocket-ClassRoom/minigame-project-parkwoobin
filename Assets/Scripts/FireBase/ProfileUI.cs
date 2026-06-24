using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ProfileUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField]
    private GameObject profilePanel;

    [Header("Profile Info")]
    [SerializeField]
    private TextMeshProUGUI nicknameText;

    [SerializeField]
    private TextMeshProUGUI bestRecordText;


    [Header("Buttons")]
    [SerializeField]
    private Button editProfileButton;

    [SerializeField]
    private Button logoutButton;

    [SerializeField]
    private Button closeProfileButton;

    [Header("References")]
    [SerializeField]
    private ProfileEditUI profileEditUI;

    private void Start()
    {
        editProfileButton.onClick.AddListener(OnEditProfileButtonClicked);
        logoutButton.onClick.AddListener(OnLogoutButtonClicked);
        closeProfileButton.onClick.AddListener(OnCloseProfileButtonClicked);

        profilePanel.SetActive(false);
    }

    public async UniTaskVoid OpenProfilePanel()
    {
        if (ProfileManager.Instance != null)
            await ProfileManager.Instance.LoadProfileAsync();

        await UpdateProfileUIAsync();

        profilePanel.SetActive(true);

        Time.timeScale = 0f;
        GameManager.Instance?.PauseGame();
    }

    public async UniTask UpdateProfileUIAsync()
    {
        string nickname = ProfileManager.Instance?.CachedProfile?.nickname ?? LocalizationManager.Get("FireBase_NoName");
        if (nicknameText != null)
            nicknameText.text = nickname;

        if (bestRecordText != null)
        {
            float best = ScoreManager.Instance?.CachedBestTime ?? float.MaxValue;
            string record = best < float.MaxValue ? TimeUtil.FormatClearTime(best) : "-";
            bestRecordText.text = string.Format(LocalizationManager.Get("FireBase_BestRecord"), record);
        }
    }

    private void OnEditProfileButtonClicked()
    {
        if (profileEditUI != null)
        {
            profilePanel.SetActive(false);
            profileEditUI.OpenProfileEditPanelAsync().Forget();
        }
    }

    private void OnCloseProfileButtonClicked()
    {
        profilePanel.SetActive(false);

        Time.timeScale = 1f;
        GameManager.Instance?.ResumeGame();
    }


    private void OnLogoutButtonClicked()
    {
        AuthManager.Instance.SignOut();
        profilePanel.SetActive(false);

        Time.timeScale = 1f;
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.TransitionTo("Main");
        else
            SceneManager.LoadScene("Main");
    }
}
