using UnityEngine;
using UnityEngine.UI;

public class TutorialFinishTrigger : MonoBehaviour
{
    [SerializeField] GameObject popupPanel;
    [SerializeField] GameObject dimOverlay;
    [SerializeField] Button nextMapButton;
    [SerializeField] string nextSceneName;

    [Header("SFX")]
    [SerializeField] AudioClip sfxClear;

    bool _triggered;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered) return;
        if (!other.CompareTag("Player")) return;

        _triggered = true;

        if (sfxClear != null && AudioManager.Instance != null)
        {
            float vol = AudioManager.Instance.SfxVolume;
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.PlayOneShot(sfxClear, vol);
        }

        if (dimOverlay != null)
        {
            dimOverlay.SetActive(true);
            dimOverlay.transform.SetAsFirstSibling();
        }
        if (popupPanel != null)
        {
            popupPanel.SetActive(true);
            popupPanel.transform.SetAsLastSibling();
        }
        if (nextMapButton != null) nextMapButton.onClick.AddListener(OnNextMap);

        Time.timeScale = 0f;
        GameManager.Instance?.PauseGame();
    }

    void OnNextMap()
    {
        if (nextMapButton != null) nextMapButton.onClick.RemoveListener(OnNextMap);
        if (popupPanel != null) popupPanel.SetActive(false);
        if (dimOverlay != null) dimOverlay.SetActive(false);

        Time.timeScale = 1f;
        GameManager.Instance?.ResumeGame();

        // 다음 씬 이름으로 저장 후 이동
        var player = FindFirstObjectByType<PlayerController>();
        var sm = SaveManager.Instance;
        if (sm != null)
        {
            var hunger = FindFirstObjectByType<HungerSystem>();
            var coinKey = CoinKeySystem.Instance;
            var gs = GameState.Instance;

            var skill = SkillUnlockManager.Instance;
            var data = new SaveData
            {
                sceneName       = nextSceneName,
                spawnAtDefault  = true,   // 다음 씬 기본 스폰 포인트에서 등장
                posX            = 0f,
                posY            = 0f,
                stage           = gs != null ? gs.savedStage : 1,
                coins           = coinKey != null ? coinKey.Coins : 0,
                hp              = player != null ? player.Hp : 0,
                maxHp           = player != null ? player.MaxHp : 0,
                key             = coinKey != null ? coinKey.Keys : 0,
                hunger          = hunger != null ? hunger.Hunger : 0f,
                attack          = player != null ? player.AttackPower : 1,
                skillAttack     = skill != null && skill.attack,
                skillJump       = skill != null && skill.jump,
                skillDash       = skill != null && skill.dash,
                skillTurn       = skill != null && skill.turn,
                skillDoubleJump = skill != null && skill.doubleJump,
                skillWallJump   = skill != null && skill.wallJump,
                upgradeLevels   = UpgradeManager.Instance?.GetLevels(),
            };
            sm.SaveGame(sm.ActiveSlot, data);
        }

        SceneTransitionManager.Instance.TransitionTo(nextSceneName);
    }
}
