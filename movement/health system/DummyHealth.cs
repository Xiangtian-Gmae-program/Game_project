using UnityEngine;
using UnityEngine.UI;

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

    void Awake()
    {
        currentHealth = maxHealth;
        animator = GetComponentInChildren<Animator>();
        UpdateHealthUI();
    }

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

        // 这里你可以二选一：
        // 1. 保留血条显示为 0
        // 2. 直接隐藏血条
        // 如果你想隐藏，就取消下面这行注释
        // if (healthCanvas != null) healthCanvas.enabled = false;
    }

    void UpdateHealthUI()
    {
        if (healthFill != null)
        {
            healthFill.fillAmount = (float)currentHealth / maxHealth;
        }
    }
}