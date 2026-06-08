// Script note: Controls simple target health, hit feedback, death state, and health UI.
// Comment pass: documents responsibilities and key entry points without changing runtime logic.
using UnityEngine;
using UnityEngine.UI;

// Class responsibility: DummyHealth contains this script's gameplay behavior.
public class DummyHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("UI")]
    public Image healthFill;
    public Canvas healthCanvas;

    private Animator animator;
    private bool isDead = false;

    // Initializes component references and runtime-only setup.
    void Awake()
    {
        currentHealth = maxHealth;
        animator = GetComponentInChildren<Animator>();
        UpdateHealthUI();
    }

    // Applies damage and handles block, evade, hit, and death outcomes.
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"{gameObject.name} took {damage} damage. Current HP: {currentHealth}");

        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            if (animator != null)
            {
                animator.SetTrigger("Hit");
            }
        }
    }

    // Handles death state and related visual feedback.
    void Die()
    {
        isDead = true;

        Debug.Log($"{gameObject.name} is dead.");

        if (animator != null)
        {
            animator.ResetTrigger("Hit");
            animator.SetBool("IsDead", true);
        }

        UpdateHealthUI();

        // ��������Զ�ѡһ��
        // 1. ����Ѫ����ʾΪ 0
        // 2. ֱ������Ѫ��
        // ����������أ���ȡ����������ע��
        // if (healthCanvas != null) healthCanvas.enabled = false;
    }

    // Refreshes health UI values.
    void UpdateHealthUI()
    {
        if (healthFill != null)
        {
            healthFill.fillAmount = (float)currentHealth / maxHealth;
        }
    }
}