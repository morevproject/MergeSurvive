using UnityEngine;
using System; // Нужно для Action
using System.Collections;

public class FighterStats : MonoBehaviour
{
    // --- НОВОЕ: Событие, чтобы UI (HealthBar) знал, когда обновиться ---
    public event Action<int> OnHealthChanged;
    // ------------------------------------------------------------------

    [Header("Base Stats")]
    public string unitName = "Unit";
    public int maxHealth = 100;
    
    [Header("Combat Stats")]
    public int minDamage = 1;
    public int maxDamage = 2;
    public float attackRange = 2.0f; 
    
    [Header("Current State")]
    public int currentHealth;
    public bool isDead = false;

    private void Start()
    {
        currentHealth = maxHealth;
        // Сообщаем всем подписчикам начальное здоровье
        OnHealthChanged?.Invoke(currentHealth);
    }

    public int GetDamage()
    {
        return UnityEngine.Random.Range(minDamage, maxDamage + 1);
    }

    public void UpdateWeaponStats(int min, int max, float range) 
    {
        if (max <= 0)
        {
            minDamage = 1;
            maxDamage = 2;
            attackRange = 2.0f; 
        }
        else
        {
            minDamage = min;
            maxDamage = max;
            attackRange = range > 0 ? range : 2.0f; 
        }
        Debug.Log($"[FighterStats] Статы обновлены. Урон: {min}-{max}, Дальность: {attackRange}");
    }

    public void TakeDamage(int dmg)
    {
        if (isDead) return;

        currentHealth -= dmg;

        // --- 1. Вылетающая цифра урона ---
        // Используем переменную dmg, которая пришла в аргументы
        DamagePopup.Create(transform.position + Vector3.up * 2f, dmg); 
        // ---------------------------------

        // --- 2. Уведомляем полоску здоровья ---
        OnHealthChanged?.Invoke(currentHealth);
        // -------------------------------------

        Debug.Log($"<color=red>{unitName} получил {dmg} урона. Осталось: {currentHealth}</color>");

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        
        // Уведомляем полоску здоровья при лечении
        OnHealthChanged?.Invoke(currentHealth);
        
        Debug.Log($"<color=green>{unitName} исцелился на {amount}.</color>");
    }

    public void ApplyBurn(int damagePerRound, int durationRounds)
    {
        StartCoroutine(BurnRoutine(damagePerRound, durationRounds));
    }

    private IEnumerator BurnRoutine(int dmg, int rounds)
    {
        yield return null; 
    }

    protected virtual void Die()
    {
        isDead = true;
    }
}