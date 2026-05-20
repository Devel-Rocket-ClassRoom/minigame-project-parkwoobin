using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private GameObject visualRoot;

    private EnemyBase enemy;
    private float hideTimer;

    void Awake()
    {
        enemy = GetComponentInParent<EnemyBase>();
        if (visualRoot != null) visualRoot.SetActive(false);
    }

    void OnEnable()
    {
        if (enemy != null)
        {
            enemy.OnHealthChanged += UpdateHealthBar;
        }
    }

    void OnDisable()
    {
        if (enemy != null)
        {
            enemy.OnHealthChanged -= UpdateHealthBar;
        }
    }

    void Update()
    {
        // Keep the health bar scale absolute so it doesn't flip with the parent
        Vector3 parentScale = transform.parent.localScale;
        transform.localScale = new Vector3(
            Mathf.Sign(parentScale.x) * 0.01f,
            0.01f,
            1f
        );

        if (hideTimer > 0f)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f && visualRoot != null)
            {
                visualRoot.SetActive(false);
            }
        }
    }

    private void UpdateHealthBar(int currentHp, int maxHp)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHp;
            healthSlider.value = currentHp;
        }

        if (visualRoot != null)
        {
            visualRoot.SetActive(true);
        }
    }
}
