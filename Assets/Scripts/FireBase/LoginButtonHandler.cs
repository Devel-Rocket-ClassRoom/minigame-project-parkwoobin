using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class LoginButtonHandler : MonoBehaviour
{
    [SerializeField] private LoginUI loginUI;

    private void Start()
    {
        if (loginUI == null)
            loginUI = FindFirstObjectByType<LoginUI>();
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    public void OnClick()
    {
        loginUI?.OpenLoginPanelAsync().Forget();
    }
}
