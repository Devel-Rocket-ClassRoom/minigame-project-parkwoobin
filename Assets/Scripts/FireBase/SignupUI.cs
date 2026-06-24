using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SignupUI : MonoBehaviour
{
    [Header("Outer Panel")]
    [SerializeField] private GameObject signupPanel;

    [Header("Step 1 - 이메일/비밀번호")]
    [SerializeField] private GameObject signupFormPanel;
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TextMeshProUGUI formErrorText;

    [Header("References")]
    [SerializeField] private ProfileEditUI profileEditUI;
    [SerializeField] private LoginUI loginUI;

    private void Start()
    {
        nextButton?.onClick.AddListener(OnNextClicked);
        cancelButton?.onClick.AddListener(() => signupPanel.SetActive(false));
    }

    private void OnNextClicked()
    {
        string email = emailInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            formErrorText.text = LocalizationManager.Get("FireBase_InPut_IDPW");
            formErrorText.color = Color.red;
            return;
        }

        formErrorText.text = "";
        signupPanel.SetActive(false);
        profileEditUI?.OpenForSignup(email, password, loginUI);
    }
}
